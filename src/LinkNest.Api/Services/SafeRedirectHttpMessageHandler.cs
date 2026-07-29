using System.Net;

namespace LinkNest.Api.Services;

/// <summary>
/// HTTP message handler that manually follows redirects while validating each target against SSRF rules.
/// </summary>
public sealed class SafeRedirectHttpMessageHandler : DelegatingHandler
{
    private const int MaxRedirects = 5;

    private readonly ISafeUrlValidator _urlValidator;

    public SafeRedirectHttpMessageHandler(ISafeUrlValidator urlValidator, HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
        _urlValidator = urlValidator;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var currentRequest = request;
        HttpResponseMessage? response = null;

        for (var redirectCount = 0; redirectCount <= MaxRedirects; redirectCount++)
        {
            if (currentRequest.RequestUri is null
                || !await _urlValidator.IsAllowedUrlAsync(currentRequest.RequestUri, cancellationToken))
            {
                response?.Dispose();
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            response?.Dispose();
            response = await base.SendAsync(currentRequest, cancellationToken);

            if (!IsRedirectStatusCode(response.StatusCode))
            {
                return response;
            }

            var location = response.Headers.Location;
            if (location is null)
            {
                return response;
            }

            var redirectUri = location.IsAbsoluteUri
                ? location
                : new Uri(currentRequest.RequestUri, location);

            if (redirectCount == MaxRedirects)
            {
                response.Dispose();
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            if (!ReferenceEquals(currentRequest, request))
            {
                currentRequest.Dispose();
            }

            currentRequest = CloneRedirectRequest(request, redirectUri);
        }

        response?.Dispose();
        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private static bool IsRedirectStatusCode(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.Moved
            or HttpStatusCode.Redirect
            or HttpStatusCode.SeeOther
            or HttpStatusCode.RedirectMethod
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;

    private static HttpRequestMessage CloneRedirectRequest(HttpRequestMessage original, Uri redirectUri)
    {
        var redirectRequest = new HttpRequestMessage(HttpMethod.Get, redirectUri);

        foreach (var header in original.Headers)
        {
            redirectRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return redirectRequest;
    }
}
