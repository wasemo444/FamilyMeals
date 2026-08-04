using LinkNest.Shared.Auth;
using LinkNest.Web.Client;
using LinkNest.Web.Client.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

InteractiveRenderSettings.ConfigureStaticWebRenderModes();

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddTransient<BearerTokenHandler>();
builder.Services.AddLinkNestStaticWebClientServices(builder.Configuration, builder.HostEnvironment.BaseAddress);

var host = builder.Build();
await host.RunAsync();
