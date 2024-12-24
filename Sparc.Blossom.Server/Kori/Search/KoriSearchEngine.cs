namespace Sparc.Kori;

public record KoriSearch(string Id, string Tag, string Text, string Domain, string Path, string Source);

public class KoriSearchEngine(KoriHttpEngine http, KoriJsEngine js)
{
    public async Task OpenAsync()
    {
        await js.InvokeVoidAsync("showSidebar");
    }

    public async Task CloseAsync()
    {
        Console.WriteLine("Closing search side bar in Kori.cs");
        await js.InvokeVoidAsync("closeSearch");
    }  

    public async Task<List<KoriSearch>> SearchAsync(string searchTerm)
    {
        var contentResults = await http.SearchContentAsync(searchTerm);
        var pageResults = await http.SearchPageAsync(searchTerm);

        var combinedResults = contentResults.Concat(pageResults).ToList();

        return combinedResults;
    }
}
