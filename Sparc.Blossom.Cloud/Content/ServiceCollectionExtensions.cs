namespace Sparc.Blossom.Content;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddBlossomCloudTranslation(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddScoped<ITranslator, AzureTranslator>()
            //.AddScoped<ITranslator, DeepLTranslator>()
            .AddScoped<BlossomTranslator>();
        
        return builder;
    }

    public static WebApplication UseBlossomCloudTranslation(this WebApplication app)
    {
        var translator = app.MapGroup("/translate");
        translator.MapGet("languages", async (BlossomTranslator translator) => await translator.GetLanguagesAsync());
        //translator.MapGet("translate", async (BlossomTranslator translator, string text, string toLanguage) => await translator.TranslateAsync(text, toLanguage));
        //translator.MapGet("detect", async (BlossomTranslator translator, string text) => await translator.DetectLanguageAsync(text));
        
        return app;
    }
}
