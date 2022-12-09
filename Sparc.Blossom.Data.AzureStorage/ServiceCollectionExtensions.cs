using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;

namespace Sparc.Blossom.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureStorage(this IServiceCollection services, string connectionString)
    {
        services.AddScoped(_ => new BlobServiceClient(connectionString));
        services.AddScoped<IFileRepository<File>, AzureBlobRepository>();
        return services;
    }
}
