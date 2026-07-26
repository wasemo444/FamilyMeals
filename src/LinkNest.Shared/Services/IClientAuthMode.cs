namespace LinkNest.Shared.Services;

/// <summary>
/// Indicates whether the client uses bearer/JWT auth (mobile) or cookie auth (web).
/// </summary>
public interface IClientAuthMode
{
    /// <summary>When true, login/logout flows use JWT bearer tokens instead of cookie form posts.</summary>
    bool UsesBearerToken { get; }
}
