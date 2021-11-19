using Microsoft.Extensions.DependencyInjection;
using System;

namespace Sparc.Authentication.Client
{
    public static class ServiceCollectionExtensions
    {
        public static ApiClient AddSelfHostedApi<T>(this IServiceCollection services, string apiName, string baseUrl, string clientId) where T : class
        {
            var client = new ApiClient(apiName, baseUrl);
            //await client.RegisterClientAsync(clientId);

            services.AddScoped(x => (T)Activator.CreateInstance(typeof(T), "", client));

            return client;
        }
    }
}
