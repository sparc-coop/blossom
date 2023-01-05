using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sparc.Blossom;
using TemplateWebNET7.Api;
using Blazored.Modal;
using TemplateWebNET7.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredModal();

builder.AddBlossom<TemplateWebNET7Api>(builder.Configuration["ApiUrl"]);

await builder.Build().RunAsync();


