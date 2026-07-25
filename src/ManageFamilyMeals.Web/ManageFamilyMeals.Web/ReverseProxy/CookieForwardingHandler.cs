namespace ManageFamilyMeals.Web.ReverseProxy;

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
