using HtmlAgilityPack;

namespace Sparc.Blossom.Content;

public class HtmlTranslator(string url)
{
    public async Task<string> TranslateAsync()
    {
        var handler = new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All
        };

        var client = new HttpClient(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; https://engine.sparc.coop)");
        var html = await client.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var body = doc.DocumentNode.SelectSingleNode("//body");

        RemoveUnneededTags(body);

        return body.InnerHtml;
    }

    private static void RemoveUnneededTags(HtmlNode body)
    {
        // Clean up all unneeded tags, keep only text and basic formatting
        var tagsToRemove = new[] { "script", "style", "iframe", "noscript", "svg", "input" };
        foreach (var tag in tagsToRemove)
            foreach (var element in body.SelectNodes("//" + tag) ?? Enumerable.Empty<HtmlNode>())
                element.Remove();

        var attributesToRemove = new[] { "class", "id", "style", "width", "height" };
        foreach (var element in body.DescendantsAndSelf())
            foreach (var attribute in attributesToRemove.Where(x => element.Attributes.Contains(x)).ToList())
                element.Attributes.Remove(attribute);
    }
}
