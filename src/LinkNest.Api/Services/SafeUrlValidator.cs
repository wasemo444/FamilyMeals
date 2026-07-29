using System.Net;
using System.Net.Sockets;

namespace LinkNest.Api.Services;

/// <summary>
/// Validates that HTTP(S) URLs resolve only to public IP addresses.
/// </summary>
/// <remarks>
/// Blocks loopback, link-local, private, and cloud metadata-range addresses, including DNS names that resolve to them.
/// </remarks>
public sealed class SafeUrlValidator : ISafeUrlValidator
{
    /// <inheritdoc />
    public async Task<bool> IsAllowedUrlAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        if (!IsAllowedScheme(uri))
        {
            return false;
        }

        if (IsBlockedHost(uri.DnsSafeHost))
        {
            return false;
        }

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
        }
        catch (SocketException)
        {
            return false;
        }

        return addresses.Length > 0 && addresses.All(address => IsPublicAddress(address));
    }

    /// <summary>
    /// Normalizes IPv4-mapped and IPv4-compatible IPv6 literals to IPv4 for range checks.
    /// </summary>
    internal static IPAddress NormalizeAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            return address.MapToIPv4();
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return address;
        }

        var bytes = address.GetAddressBytes();
        var isCompatible = true;
        for (var i = 0; i < 10; i++)
        {
            if (bytes[i] != 0)
            {
                isCompatible = false;
                break;
            }
        }

        if (isCompatible && bytes[10] == 0 && bytes[11] == 0)
        {
            return new IPAddress(new byte[] { bytes[12], bytes[13], bytes[14], bytes[15] });
        }

        return address;
    }

    internal static bool IsAllowedScheme(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;

    internal static bool IsBlockedHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return true;
        }

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IPAddress.TryParse(host, out var parsed) && !IsPublicAddress(parsed);
    }

    internal static bool IsPublicAddress(IPAddress address)
    {
        address = NormalizeAddress(address);

        if (IPAddress.IsLoopback(address)
            || address.IsIPv6LinkLocal
            || address.IsIPv6SiteLocal
            || address.IsIPv6UniqueLocal)
        {
            return false;
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return address.AddressFamily == AddressFamily.InterNetworkV6;
        }

        var bytes = address.GetAddressBytes();

        return bytes[0] switch
        {
            10 => false,
            127 => false,
            0 => false,
            100 when bytes[1] is >= 64 and <= 127 => false,
            169 when bytes[1] == 254 => false,
            172 when bytes[1] is >= 16 and <= 31 => false,
            192 when bytes[1] == 168 => false,
            _ => true
        };
    }
}
