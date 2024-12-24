using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Net.Http.Json;

namespace Sparc.Kori;

public record KoriLanguage(string Id, string DisplayName, string NativeName, bool IsRightToLeft);
public record AzureLanguageList(Dictionary<string, AzureLanguageItem> translation);//dictionary of languages //List<LanguageItem>> translation);//
public record AzureLanguageItem(string name, string nativeName, string dir);
public class KoriLanguageEngine
{
    public KoriLanguageEngine(IConfiguration config)
    {
        AzureClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.cognitive.microsofttranslator.com"),
        };
        //AzureClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", config.GetConnectionString("Cognitive"));
        //AzureClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Region", "southcentralus");
    }
    
    public List<KoriLanguage> AllLanguages { get; set; } = [];
    public KoriLanguage Value { get; set; } = new("en", "English", "English", false);
    public HttpClient AzureClient { get; }

    public KoriLanguage Current => AllLanguages.First(x => x.Id == Value.Id);

    public async Task<List<KoriLanguage>> InitializeAsync(KoriLanguage? selectedLanguage)
    {
        if (AllLanguages.Count == 0)
        {
            var azureLanguages = await AzureClient.GetFromJsonAsync<AzureLanguageList>("/languages?api-version=3.0&scope=translation");
            AllLanguages = azureLanguages!.translation
                .Select(x => new KoriLanguage(x.Key, x.Value.name, x.Value.nativeName, x.Value.dir == "rtl"))
                .ToList();
        }

        AllLanguages = AllLanguages.OrderBy(x => x.DisplayName).ToList();

        var selectedLanguageId = selectedLanguage?.Id ?? CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        Value = AllLanguages.FirstOrDefault(x => x.Id == selectedLanguageId) ?? Value;
        
        return AllLanguages;
    }

}

