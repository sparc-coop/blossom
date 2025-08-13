namespace Sparc.Blossom.Content;

public static class ContentServiceCollectionExtensions
{
    public static WebApplicationBuilder AddTovikTranslator(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddScoped<ITranslator, AzureTranslator>()
            .AddScoped<ITranslator, DeepLTranslator>()
            .AddScoped<TovikTranslator>()
            .AddScoped<BlossomAggregateOptions<TextContent>>()
            .AddScoped<BlossomAggregate<TextContent>>()
            .AddScoped<Contents>();

        return builder;
    }
}
