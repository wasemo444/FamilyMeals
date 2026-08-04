using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace LinkNest.Web.Client;

/// <summary>
/// Shared render mode settings for Blazor Web and MAUI Hybrid hosts.
/// </summary>
public static class InteractiveRenderSettings
{
    public static IComponentRenderMode? PageRenderMode { get; set; } = RenderMode.InteractiveServer;

    public static IComponentRenderMode? AutoRenderMode { get; set; } = RenderMode.InteractiveAuto;

    public static IComponentRenderMode? RegisterRenderMode { get; set; } = new InteractiveServerRenderMode(prerender: false);

    /// <summary>
    /// Clears render modes so components run interactively in MAUI BlazorWebView.
    /// </summary>
    public static void ConfigureBlazorHybridRenderModes()
    {
        PageRenderMode = null;
        AutoRenderMode = null;
        RegisterRenderMode = null;
    }

    /// <summary>
    /// Clears render modes for standalone Blazor WebAssembly (entire app runs in WASM).
    /// Do not use InteractiveWebAssembly render modes here — those apply to Blazor Web App hosts only.
    /// </summary>
    public static void ConfigureStaticWebRenderModes()
    {
        PageRenderMode = null;
        AutoRenderMode = null;
        RegisterRenderMode = null;
    }
}
