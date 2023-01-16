using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sparc.Blossom;
using Blazored.Modal;
using TemplateMAUINET7.Web;
using TemplateMAUINET7.Features;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredModal();

builder.AddBlossom<TemplateMAUINET7Client>(builder.Configuration["ApiUrl"]);

await builder.Build().RunAsync();


