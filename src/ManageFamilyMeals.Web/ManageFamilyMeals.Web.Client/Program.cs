using ManageFamilyMeals.Web.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddManageFamilyMealsClientServices(builder.Configuration);

await builder.Build().RunAsync();
