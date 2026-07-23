namespace Sparc.Blossom.Content;

public static class ContentServiceCollectionExtensions
{
    public static WebApplicationBuilder AddSparcContent(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddScoped<AITranslator, AzureTranslator>()
            .AddScoped<AITranslator, DeepLTranslator>()
            .AddScoped<AITranslator, OpenAITranslator>()
            .AddScoped<AITranslator, AnthropicTranslator>()
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
