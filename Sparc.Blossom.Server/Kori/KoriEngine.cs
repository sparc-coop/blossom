using Microsoft.JSInterop;
using Microsoft.AspNetCore.Http;

namespace Sparc.Kori;

public class KoriEngine(
    KoriLanguageEngine language,
    KoriHttpEngine http,
    KoriContentEngine content,
    KoriSearchEngine search,
    KoriImageEngine images,
    KoriJsEngine js)
{
    public KoriContentRequest CurrentRequest { get; private set; } = new("", "", "");
    public static Uri BaseUri { get; set; } = new("https://localhost");
    public TagManager TagManager { get; } = new TagManager();
    public string Mode { get; set; } = "";

    public event EventHandler<EventArgs> StateChanged;

    public async Task InitializeAsync(string currentUrl)
    {
        var url = new Uri(currentUrl);

        CurrentRequest = new KoriContentRequest(BaseUri.Host, language.Value.Id, url.PathAndQuery);

        await http.InitializeAsync(CurrentRequest);
        await content.InitializeAsync(CurrentRequest);
        await images.InitializeAsync();
    }

    public async Task InitializeAsync(HttpContext context)
    {
        await InitializeAsync(context.Request.Path);
    }

    public async Task InitializeAsync(string currentUrl, string elementId)
    {
        await InitializeAsync(currentUrl);
        await js.InvokeVoidAsync("init",
            elementId,
            language.Value.Id,
            DotNetObjectReference.Create(this),
            content.Value);
    }

    public async Task ChangeMode(string mode)
    {
        if (Mode == mode)
        {
            Mode = string.Empty;
            return;
        }

        Mode = mode;

        switch (Mode)
        {
            case "Search":
                await OpenSearchMenuAsync();
                break;
            case "Language":
                OpenTranslationMenu();
                break;
            case "Blog":
                OpenBlogMenu();
                break;
            case "A/B Testing":
                OpenABTestingMenu();
                break;
            case "Edit":
                await content.BeginEditAsync();
                break;
            case "EditImage":
                await images.BeginEditAsync();
                break;
            default:
                break;
        }
    }

    [JSInvokable]
    public async Task<Dictionary<string, string>> TranslateAsync(Dictionary<string, string> newContent)
        => await content.TranslateAsync(CurrentRequest, newContent);

    [JSInvokable]
    public async Task<KoriTextContent> SaveAsync(string id, string tag, string text)
        => await content.CreateOrUpdateContentAsync(CurrentRequest, id, tag, text);

    public async Task BeginSaveAsync()
        => await content.BeginSaveAsync();

    public async Task<List<KoriSearch>> SearchAsync(string searchTerm)
        => await search.SearchAsync(searchTerm);

    public async Task CloseAsync()
    {
        Mode = "Default";
        await search.CloseAsync();
    }

    public void OpenTranslationMenu()
    {
        Mode = "Language";
    }

    public async Task OpenSearchMenuAsync()
    {
        Mode = "Search";
        await search.OpenAsync();
    }

    public void OpenBlogMenu()
    {
        Mode = "Blog";
    }

    public void OpenABTestingMenu()
    {
        Mode = "ABTesting";
    }

    public async Task ApplyMarkdown(string symbol, string position) => await js.InvokeVoidAsync("applyMarkdown", symbol, position);
    
    [JSInvokable]
    public async Task EditAsync()
    {
        var contentType = await js.InvokeAsync<string>("checkSelectedContentType");

        if (contentType == "image")
        {
            Mode = "EditImage";
            await js.InvokeVoidAsync("editImage");
        }
        else
        {
            Mode = "Edit";
            await js.InvokeVoidAsync("edit");
        }

        InvokeStateHasChanged();
    }

    private void InvokeStateHasChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    [JSInvokable]
    public void SetDefaultMode()
    {
        Mode = "Default";
        InvokeStateHasChanged();
    }
}


public class TagManager
{
    private readonly Dictionary<string, string> dict = new Dictionary<string, string>();

    public string this[string key]
    {
        get
        {
            if (!dict.ContainsKey(key))
            {
                dict.Add(key, "");
            }
            return dict[key];
        }
        set
        {
            dict[key] = value;
        }
    }
}