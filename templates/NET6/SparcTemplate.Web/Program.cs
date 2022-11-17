using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SparcTemplate.Web;
using Sparc.Authentication.Blazor;
using SparcTemplate.Features;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");


builder.Services.AddScoped<IConfiguration>(_ => builder.Configuration);

builder.AddPublicApi<SparcTemplateApi>(builder.Configuration["ApiUrl"]);

await builder.Build().RunAsync();
