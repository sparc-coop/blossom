namespace Kori;

public class Pages(BlossomAggregateOptions<Page> options) : BlossomAggregate<Page>(options)
{
    public BlossomQuery<Page> Search(string searchTerm) => Query().Where(page =>
         ((page.Domain != null && page.Domain.ToLower().Contains(searchTerm) == true) ||
         (page.Path != null && page.Path.ToLower().Contains(searchTerm) == true)));

   public async Task<Page> Register(string url, string title)
    {
        var uri = new Uri(url.ToLower());

        var page = await Get(uri.AbsoluteUri) 
            ?? await Create(uri.Host, uri.AbsolutePath, title);

        if (page.Name != title)
            await Execute(page.Id, x => x.UpdateName(title));

        return page;
    }
}