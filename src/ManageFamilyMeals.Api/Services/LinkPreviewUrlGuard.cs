using System.Net;
using System.Net.Sockets;

namespace ManageFamilyMeals.Api.Services;

public static class LinkPreviewUrlGuard
{
    public static async Task<bool> IsAllowedPublicUrlAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
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

        return addresses.Length > 0 && addresses.All(IsPublicAddress);
    }

    private static bool IsBlockedHost(string host)
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

    private static bool IsPublicAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address)
            || address.IsIPv6LinkLocal
            || address.IsIPv6SiteLocal
            || address.IsIPv6UniqueLocal)
        {
            return false;
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return true;
        }

        var bytes = address.GetAddressBytes();

        return bytes[0] switch
        {
            10 => false,
            127 => false,
            0 => false,
            169 when bytes[1] == 254 => false,
            172 when bytes[1] is >= 16 and <= 31 => false,
            192 when bytes[1] == 168 => false,
            _ => true
        };
    }
}
