using HtmlAgilityPack;
using Sparc.Blossom.Authentication;

namespace Sparc.Blossom.Content;

public record SparcCrawlResult(string Url, string Html, bool IsTovikInstalled, string? Title = null);
public class SparcCrawler
{
    HttpClient Client = new() { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/accounts/70e63b236996bce308a10f5618769282/browser-rendering/") };
    string TovikUrl = "";
    IRepository<TextContent> Contents;
    Pages Pages;    

    public SparcCrawler(IConfiguration config, IRepository<TextContent> contents, Pages pages)
    {
        var apiKey = config.GetConnectionString("Cloudflare");
        Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        TovikUrl = config["Tovik"]!;
        Contents = contents;
        Pages = pages;
    }

    record CrawlRequest(string url, int limit, int depth, List<string> crawlPurposes, GotoOptions gotoOptions);
    record CrawlMetadata(int status, string url, string? title = null);
    record CrawlResult(CrawlMetadata metadata, string status, string url, string? html = null);
    record CrawlResponseWrapper(CrawlResponse result);
    record CrawlResponse(string id, int finished, List<CrawlResult> records, string status, int total, int? cursor = null);
    record CrawlJob(bool success, string result);
    public async Task<string> BeginCrawlAsync(SparcDomain domain)
    {
        var request = new CrawlRequest(domain.ToAbsoluteUrl(), 100, 4, ["search"], new("networkidle0"));
        var result = await Client.PostAsJsonAsync<CrawlJob>("crawl", request);
        if (result?.success != true)
            throw new Exception("Crawl failed");

        return result.result;
    }

    public async Task<bool> WaitForCrawlAsync(string jobId)
    {
        var response = await Client.GetFromJsonAsync<CrawlResponseWrapper>($"crawl/{jobId}?limit=1");
        while (true)
        {
            switch (response?.result.status)
            {
                case "completed":
                    var results = await GetCrawlResultsAsync(jobId);
                    await Parallel.ForEachAsync(results, async (result, _) => await ExtractContentAsync(result));
                    return true;
                case "running":
                    await Task.Delay(5000);
                    response = await Client.GetFromJsonAsync<CrawlResponseWrapper>($"crawl/{jobId}?limit=1");
                    break;
                default:
                    return false;
            }
        }
    }

    public async Task<List<SparcCrawlResult>?> GetCrawlResultsAsync(string jobId)
    {
        var crawlResults = new List<SparcCrawlResult>();
        int? cursor = null;
        do
        {
            var response = await Client.GetFromJsonAsync<CrawlResponseWrapper>($"crawl/{jobId}?status=completed" + (cursor != null ? $"&cursor={cursor}" : ""));
            if (response == null)
                break;

            foreach (var result in response.result.records)
            {
                if (result.status == "completed" && result.html != null)
                {
                    var isTovikInstalled = result.html.Contains("tovik.js") || result.html.Contains("kori.js");
                    crawlResults.Add(new(result.url, result.html, isTovikInstalled, result.metadata.title));
                }
            }

            cursor = response.result.cursor;
        } while (cursor != null);

        return crawlResults;
    }

    async Task ExtractContentAsync(SparcCrawlResult crawlResult)
    {
        var uri = new Uri(crawlResult.Url);
        
        var content = new List<TextContent>();
        var doc = new HtmlDocument();
        doc.LoadHtml(crawlResult.Html);

        // Get language
        var htmlNode = doc.DocumentNode.SelectSingleNode("//html");
        var lang = htmlNode?.GetAttributeValue("lang", null);

        var page = await Pages.Register(crawlResult.Url, crawlResult.Title ?? "", lang);

        // Remove script & style nodes
        doc.DocumentNode.Descendants().Where(n => n.Name == "script" || n.Name == "style").ToList().ForEach(n => n.Remove());
        var textNodes = doc.DocumentNode.SelectNodes("//text()[normalize-space(.) != '']");

        if (textNodes != null)
        {
            foreach (HtmlNode node in textNodes)
                content.Add(new TextContent(page, node.InnerText.Trim()));

            await Contents.UpdateAsync(content);
        }
    }

    record GotoOptions(string waitUntil);
    record AddScript(string type, string url);
    record WaitFor(string selector);
    record RenderRequest(string url, string userAgent, Dictionary<string, string> setExtraHTTPHeaders, GotoOptions gotoOptions);
    record RenderError(string code, string message);
    record RenderResponse(bool success, List<RenderError>? errors, string? result);
    public async Task<SparcCrawlResult> PreviewDynamicAsync(SparcDomain domain, Page page, string lang)
    {
        var domainUri = domain.ToUri();
        var url = page.AbsolutePath();

        var request = new RenderRequest(url,
            "Mozilla/5.0 (compatible; https://tovik.app)",
            //[new("module", $"{TovikUrl}/tovik.js")],
            new Dictionary<string, string> { { "Accept-Language", lang } },
            new("networkidle0"));

        var result = await Client.PostAsJsonAsync<RenderResponse>("content", request);
        if (result == null || result.errors != null || result.result == null)
            throw new Exception("Failed to render page: " + (result?.errors != null ? string.Join(", ", result.errors.Select(e => e.message)) : "Unknown error"));

        var isTovikInstalled = result.result?.Contains("tovik.js") == true;

        var doc = new HtmlDocument();
        doc.LoadHtml(result.result!);

        // Inject tovik.js script
        var body = doc.DocumentNode.SelectSingleNode("//body");
        if (body != null)
        {
            if (!isTovikInstalled)
            {
                var script = doc.CreateElement("script");
                script.SetAttributeValue("type", "module");
                script.SetAttributeValue("src", $"{TovikUrl}/tovik.js");
                body.AppendChild(script);
            }

            body.SetAttributeValue("data-tovikdomain", domain.Domain);
            body.SetAttributeValue("data-tovikpath", page.Path);

            // Inject lang into data-lang attribute of html
            if (lang != null)
                body.SetAttributeValue("data-toviklang", lang);
        }

        // Convert all relative links to absolute using base tag
        var baseTag = doc.DocumentNode.SelectSingleNode("//head/base");
        if (baseTag == null)
        {
            var head = doc.DocumentNode.SelectSingleNode("//head");
            if (head == null)
            {
                head = doc.CreateElement("head");
                doc.DocumentNode.PrependChild(head);
            }
            baseTag = doc.CreateElement("base");
            head.PrependChild(baseTag);
        }
        baseTag.SetAttributeValue("href", domainUri.GetLeftPart(UriPartial.Authority));

        // Rewrite links to open in this same Preview.razor
        var links = doc.DocumentNode.SelectNodes("//a[@href]");
        if (links != null)
        {
            foreach (var link in links)
            {
                var href = link.GetAttributeValue("href", "");
                // make the href absolute if needed
                if (Uri.TryCreate(href, UriKind.RelativeOrAbsolute, out var relativeUri) && !relativeUri.IsAbsoluteUri)
                    href = new Uri(domainUri, relativeUri).ToString();

                link.SetAttributeValue("href", "#");
                link.SetAttributeValue("onclick", $"window.parent.postMessage('tovik-url:{href}'); return false;");
            }
        }

        var outerHtml = doc.DocumentNode.OuterHtml;
        return new(url, outerHtml, isTovikInstalled);
    }

    public async Task<SparcCrawlResult> PreviewAsync(SparcDomain domain, Page page, string? lang = null)
    {
        var domainUri = domain.ToUri();

        var handler = new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All
        };

        var client = new HttpClient(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; https://tovik.app)");
        var url = page.AbsolutePath(lang);
        string html = await client.GetStringAsync(url);
        var isTovikInstalled = html.Contains("tovik.js");

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Inject tovik.js script
        var body = doc.DocumentNode.SelectSingleNode("//body");
        if (body != null)
        {
            if (!isTovikInstalled)
            {
                var script = doc.CreateElement("script");
                script.SetAttributeValue("type", "module");
                script.SetAttributeValue("src", $"{TovikUrl}/tovik.js");
                body.AppendChild(script);
            }

            body.SetAttributeValue("data-tovikdomain", domain.Domain);
            body.SetAttributeValue("data-tovikpath", page.Path);

            // Inject lang into data-lang attribute of html
            if (lang != null)
                body.SetAttributeValue("data-toviklang", lang);
        }

        // Convert all relative links to absolute using base tag
        var baseTag = doc.DocumentNode.SelectSingleNode("//head/base");
        if (baseTag == null)
        {
            var head = doc.DocumentNode.SelectSingleNode("//head");
            if (head == null)
            {
                head = doc.CreateElement("head");
                doc.DocumentNode.PrependChild(head);
            }
            baseTag = doc.CreateElement("base");
            head.PrependChild(baseTag);
        }
        baseTag.SetAttributeValue("href", domainUri.GetLeftPart(UriPartial.Authority));

        // Rewrite links to open in this same Preview.razor
        var links = doc.DocumentNode.SelectNodes("//a[@href]");
        if (links != null)
        {
            foreach (var link in links)
            {
                var href = link.GetAttributeValue("href", "");
                // make the href absolute if needed
                if (Uri.TryCreate(href, UriKind.RelativeOrAbsolute, out var relativeUri) && !relativeUri.IsAbsoluteUri)
                    href = new Uri(domainUri, relativeUri).ToString();

                link.SetAttributeValue("href", "#");
                link.SetAttributeValue("onclick", $"window.parent.postMessage('tovik-url:{href}'); return false;");
            }
        }

        var result = doc.DocumentNode.OuterHtml;
        return new(url, result, isTovikInstalled);
    }
}
