using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sparc.Blossom.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureStorage(this IServiceCollection services, string connectionString)
    {
        services.AddScoped(_ => new BlobServiceClient(connectionString));
        services.AddScoped<IRepository<BlossomFile>, AzureBlobRepository>();
        return services;
    }

    public static IServiceCollection AddAzureStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped(_ => new BlobServiceClient(configuration.GetConnectionString("Storage")));
        services.AddScoped<IRepository<BlossomFile>, AzureBlobRepository>();
        return services;
    }
}
