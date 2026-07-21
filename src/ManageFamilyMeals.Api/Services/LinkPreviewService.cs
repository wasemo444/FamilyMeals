using System.Net;
using System.Text.RegularExpressions;
using ManageFamilyMeals.Shared.Models;

namespace ManageFamilyMeals.Api.Services;

public sealed class LinkPreviewService(IHttpClientFactory httpClientFactory, ILogger<LinkPreviewService> logger)
{
    private const int MaxHtmlBytes = 256 * 1024;
    private const int MaxImageBytes = 5 * 1024 * 1024;

    private static readonly Regex MetaTagRegex = new(
        "<meta\\s+(?:[^>]*?(?:property|name)\\s*=\\s*[\"'](?<key>[^\"']+)[\"'][^>]*?content\\s*=\\s*[\"'](?<content>[^\"']*)[\"']|[^>]*?content\\s*=\\s*[\"'](?<content2>[^\"']*)[\"'][^>]*?(?:property|name)\\s*=\\s*[\"'](?<key2>[^\"']+)[\"'])[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex TitleRegex = new(
        "<title[^>]*>(?<title>.*?)</title>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex LinkImageRegex = new(
        "<link[^>]+rel\\s*=\\s*[\"'](?:image_src|apple-touch-icon|icon)[\"'][^>]+href\\s*=\\s*[\"'](?<href>[^\"']+)[\"']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<LinkPreviewData?> FetchAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return null;
        }

        if (!await LinkPreviewUrlGuard.IsAllowedPublicUrlAsync(uri, cancellationToken))
        {
            logger.LogWarning("Blocked link preview fetch for non-public URL {Url}", url);
            return null;
        }

        try
        {
            var client = httpClientFactory.CreateClient(nameof(LinkPreviewService));
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var html = await ReadHtmlAsync(response, cancellationToken);
            if (string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            var metadata = ParseMetadata(html);
            var imageUrl = ResolveImageUrl(uri, metadata, html);

            return new LinkPreviewData
            {
                Title = FirstNonEmpty(
                    metadata,
                    "og:title",
                    "twitter:title",
                    "title")
                    ?? ExtractTitle(html),
                Description = FirstNonEmpty(
                    metadata,
                    "og:description",
                    "twitter:description",
                    "description"),
                ImageUrl = imageUrl,
                SiteName = FirstNonEmpty(metadata, "og:site_name") ?? uri.Host
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch link preview for {Url}", url);
            return null;
        }
    }

    public async Task<(byte[] Bytes, string ContentType)?> FetchImageAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return null;
        }

        if (!await LinkPreviewUrlGuard.IsAllowedPublicUrlAsync(uri, cancellationToken))
        {
            logger.LogWarning("Blocked link preview fetch for non-public URL {Url}", url);
            return null;
        }

        try
        {
            var client = httpClientFactory.CreateClient(nameof(LinkPreviewService));
            using var response = await client.GetAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var bytes = await ReadBytesWithLimitAsync(response, MaxImageBytes, cancellationToken);
            return bytes is null or { Length: 0 } ? null : (bytes, contentType);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch preview image for {Url}", url);
            return null;
        }
    }

    private static async Task<byte[]?> ReadBytesWithLimitAsync(
        HttpResponseMessage response,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];

        while (true)
        {
            var read = await stream.ReadAsync(chunk.AsMemory(0, chunk.Length), cancellationToken);
            if (read == 0)
            {
                break;
            }

            buffer.Write(chunk, 0, read);
            if (buffer.Length > maxBytes)
            {
                return null;
            }
        }

        return buffer.ToArray();
    }

    private static async Task<string> ReadHtmlAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        var buffer = new char[MaxHtmlBytes];
        var read = await reader.ReadAsync(buffer.AsMemory(0, MaxHtmlBytes), cancellationToken);
        return new string(buffer, 0, read);
    }

    private static Dictionary<string, string> ParseMetadata(string html)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in MetaTagRegex.Matches(html))
        {
            var key = match.Groups["key"].Success ? match.Groups["key"].Value : match.Groups["key2"].Value;
            var content = match.Groups["content"].Success ? match.Groups["content"].Value : match.Groups["content2"].Value;

            if (!string.IsNullOrWhiteSpace(key) && !metadata.ContainsKey(key))
            {
                metadata[key] = WebUtility.HtmlDecode(content);
            }
        }

        return metadata;
    }

    private static string? ResolveImageUrl(Uri pageUri, Dictionary<string, string> metadata, string html)
    {
        var image = FirstNonEmpty(
            metadata,
            "og:image:secure_url",
            "og:image:url",
            "og:image",
            "twitter:image:src",
            "twitter:image");

        if (string.IsNullOrWhiteSpace(image))
        {
            var linkMatch = LinkImageRegex.Match(html);
            if (linkMatch.Success)
            {
                image = linkMatch.Groups["href"].Value;
            }
        }

        return MakeAbsoluteUrl(pageUri, image);
    }

    private static string? FirstNonEmpty(Dictionary<string, string> metadata, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? ExtractTitle(string html)
    {
        var match = TitleRegex.Match(html);
        return match.Success ? WebUtility.HtmlDecode(match.Groups["title"].Value.Trim()) : null;
    }

    private static string? MakeAbsoluteUrl(Uri baseUri, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = WebUtility.HtmlDecode(value.Trim());

        return Uri.TryCreate(value, UriKind.Absolute, out _)
            ? value
            : new Uri(baseUri, value).ToString();
    }
}
