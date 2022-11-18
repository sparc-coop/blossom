using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sparc.Blossom.Web;
using TemplateWebNET7.Api;
using Blazored.Modal;

namespace TemplateWebNET7.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.AddBlazoredModal();

            builder.AddBlossom<TemplateWebNET7Api>(builder.Configuration["ApiUrl"]);

            await builder.Build().RunAsync();
        }
    }
}

