using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sparc.Blossom;
using Blazored.Modal;
using TemplateWebNET7.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredModal();

builder.AddBlossom<TemplateWebNET7Client>(builder.Configuration["ApiUrl"]);

await builder.Build().RunAsync();


