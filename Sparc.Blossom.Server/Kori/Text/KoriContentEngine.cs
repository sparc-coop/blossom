using MediatR;
using System.Text;

namespace Sparc.Kori;

public record KoriPage(string Name, string Domain, string Path, List<string> Languages, ICollection<KoriTextContent> Content, string Id);
public record KoriTextContent(string Id, string Tag, string Language, string Text, string Html, string ContentType, KoriAudio? Audio, List<object>? Nodes, bool Submitted = true);
public record KoriAudio(string Url, long Duration, string Voice, ICollection<KoriWord> Subtitles);
public record KoriWord(string Text, long Duration, long Offset);

public class KoriContentEngine(KoriHttpEngine http, KoriJsEngine js)
{
    public Dictionary<string, KoriTextContent> Value { get; set; } = [];
    public string EditMode { get; set; } = "Edit";

    public async Task InitializeAsync(KoriContentRequest request)
    {
        var page = await GetOrCreatePage(request);

        //TODO check if lang is added

        Value = await http.GetContentAsync(request.Domain, request.Path) ?? [];
    }

    private async Task<KoriPage> GetOrCreatePage(KoriContentRequest request)
    {
        var page = await http.GetPageByDomainAndPathAsync(request.Domain, request.Path);

        if (page == null)
        {
            page = await http.CreatePage(request.Domain, request.Path, "new page");
        }

        return page;
    }

    public async Task<Dictionary<string, string>> TranslateAsync(KoriContentRequest request, Dictionary<string, string> nodes)
    {
        if (nodes.Count == 0)
            return nodes;
        
        var keysToTranslate = nodes.Where(x => !Value.ContainsKey(x.Key)).Select(x => x.Key).Distinct().ToList();
        
        if (keysToTranslate.Count == 0)
            return nodes;
        
        var messagesDictionary = keysToTranslate.ToDictionary(key => key, key => nodes[key]);
        var page = await GetOrCreatePage(request);
        
        var content = await http.TranslateAsync(page.Id, messagesDictionary, request.Language);

        if (content == null)
            return nodes;

        foreach (var item in content)
        {
            Value[item.Value.Tag] = item.Value with { Nodes = [] };
        }

        foreach (var key in nodes.Keys.ToList())
        {
            if (Value.TryGetValue(key, out KoriTextContent? value))
            {
                nodes[key] = value.Text;
            }
        }

        return nodes;
    }

    public async Task<KoriTextContent> CreateOrUpdateContentAsync(KoriContentRequest request, string id, string tag, string text)
    {
        // Need to add user ABMode

        var page = await http.GetPageByDomainAndPathAsync(request.Domain, request.Path);
        if (page == null)
            throw new InvalidOperationException("Page not found for the given domain and path.");

        KoriTextContent content;

        if (string.IsNullOrEmpty(id))
        {
            content = await http.CreateContent(request.Domain, request.Path, request.Language, tag, text, "Text");
        }
        else
        {
            content = await http.GetContentByIdAsync(id);

            if (content == null)
            {
                content = await http.CreateContent(request.Domain, request.Path, request.Language, tag, text, "Text");
            }
            else
            {
                await http.SetTextAndHtmlContentAsync(content.Id, text);
                content = await http.GetContentByIdAsync(id);
                //TODO check if it's possible to return value from first http call
            }
        }

        return content;
    }

    public async Task PlayAsync(KoriTextContent content)
    {
        if (content?.Audio?.Url == null)
            return;

        await js.InvokeVoidAsync("playAudio", content.Audio.Url);
    }

    public async Task BeginEditAsync()
    {
        await js.InvokeVoidAsync("edit");
    }

    public async Task ApplyMarkdown(string symbol)
    {
        await js.InvokeVoidAsync("applyMarkdown", symbol);
    }

    public async Task BeginSaveAsync()
    {
        await js.InvokeVoidAsync("save");
    }    

    public async Task CancelAsync()
    {
        await js.InvokeVoidAsync("cancelEdit");
    }

    static string LoremIpsum(int wordCount)
    {
        var words = new[]{"lorem", "ipsum", "dolor", "sit", "amet", "consectetuer",
        "adipiscing", "elit", "sed", "diam", "nonummy", "nibh", "euismod",
        "tincidunt", "ut", "laoreet", "dolore", "magna", "aliquam", "erat"};

        var rand = new Random();
        StringBuilder result = new();

        for (int i = 0; i < wordCount; i++)
        {
            var word = words[rand.Next(words.Length)];
            var punctuation = i == wordCount - 1 ? "." : rand.Next(8) == 2 ? "," : "";

            if (i > 0)
                result.Append($" {word}{punctuation}");
            else
                result.Append($"{word[0].ToString().ToUpper()}{word.AsSpan(1)}");
        }

        return result.ToString();
    }
}
