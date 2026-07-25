using ManageFamilyMeals.Web.Client;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();
builder.Services.AddManageFamilyMealsClientServices(builder.Configuration, builder.HostEnvironment.BaseAddress);

await builder.Build().RunAsync();
