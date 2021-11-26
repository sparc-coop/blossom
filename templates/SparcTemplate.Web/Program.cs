using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SparcTemplate.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");


builder.Services.AddScoped<IConfiguration>(_ => builder.Configuration);

//await builder.Services.AddSelfHostedApi<SparcTemplateApi>(
//                    "MyContactNetwork API",
//                    builder.Configuration["ApiUrl"],
//                    "Web");


await builder.Build().RunAsync();
