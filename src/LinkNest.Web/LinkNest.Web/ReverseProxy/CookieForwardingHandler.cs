namespace LinkNest.Web.ReverseProxy;

/// <summary>
/// Copies the incoming browser cookie header onto outbound <see cref="HttpClient"/> requests.
/// </summary>
/// <remarks>
/// Registered on the <c>LinkNestApi</c> HTTP client so server-side Blazor code can call the
/// same-origin <c>/api</c> proxy with the user's authentication cookies.
/// </remarks>
public sealed class CookieForwardingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.Request.Headers.TryGetValue("Cookie", out var cookieHeader) == true
            && !string.IsNullOrWhiteSpace(cookieHeader))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader.ToString());
        }

        return base.SendAsync(request, cancellationToken);
    }
}
