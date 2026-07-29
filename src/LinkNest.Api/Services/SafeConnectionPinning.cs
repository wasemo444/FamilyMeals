using System.Net;
using System.Net.Sockets;

namespace LinkNest.Api.Services;

/// <summary>
/// Pins outbound HTTP connections to DNS-resolved public addresses to close validate-then-connect SSRF gaps.
/// </summary>
internal static class SafeConnectionPinning
{
    /// <summary>
    /// Creates a <see cref="SocketsHttpHandler.ConnectCallback"/> that connects only to validated public IPs.
    /// </summary>
    public static Func<SocketsHttpConnectionContext, CancellationToken, ValueTask<Stream>> CreateConnectCallback()
    {
        return async (context, cancellationToken) =>
        {
            var host = context.DnsEndPoint.Host;
            var port = context.DnsEndPoint.Port;

            IPAddress[] addresses;
            try
            {
                addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
            }
            catch (SocketException ex)
            {
                throw new HttpRequestException($"Blocked host: DNS resolution failed for '{host}'.", ex);
            }

            var publicAddresses = addresses
                .Select(SafeUrlValidator.NormalizeAddress)
                .Where(SafeUrlValidator.IsPublicAddress)
                .Distinct()
                .ToArray();

            if (publicAddresses.Length == 0)
            {
                throw new HttpRequestException($"Blocked host: no public addresses for '{host}'.");
            }

            var target = publicAddresses[0];
            var socket = new Socket(target.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(new IPEndPoint(target, port), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        };
    }
}
