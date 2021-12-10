using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using $ext_safeprojectname$.Web;
using Sparc.Authentication.Blazor;
using $ext_safeprojectname$.Features;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");


builder.Services.AddScoped<IConfiguration>(_ => builder.Configuration);

builder.AddB2CApi<$ext_safeprojectname$Api > (builder.Configuration["ApiScope"], builder.Configuration["ApiUrl"]);

await builder.Build().RunAsync();
