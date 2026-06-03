namespace Sparc.Blossom.Content;

public static class ContentServiceCollectionExtensions
{
    public static WebApplicationBuilder AddSparcContent(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddScoped<ITranslator, AzureTranslator>()
            .AddScoped<ITranslator, DeepLTranslator>()
            .AddScoped<ITranslator, OpenAITranslator>()
            .AddScoped<ITranslator, AnthropicTranslator>()
            .AddScoped<VoyageTranslator>()
            .AddScoped<DocumentTranslator>()
            .AddScoped<SparcCrawler>()
            .AddScoped<BlossomAggregateOptions<TextContent>>()
            .AddScoped<BlossomAggregate<TextContent>>()
            .AddScoped<Contents>()
            .AddScoped<Pages>();

        return builder;
    }
}
