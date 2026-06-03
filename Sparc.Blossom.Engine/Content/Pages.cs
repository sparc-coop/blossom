
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Content;

public class Pages(IRepository<Page> pages)
{
    //public BlossomQuery<Page> Search(string searchTerm) => pages.Query.Where(page =>
    //     ((page.Domain != null && page.Domain.ToLower().Contains(searchTerm) == true) ||
    //     (page.Path != null && page.Path.ToLower().Contains(searchTerm) == true)));

   public async Task<Page> Register(string url, string title, string? language = null)
    {
        var uri = new Uri(url.ToLower());

        var page = await pages.Query
            .Where(x => x.Domain == uri.Host && x.Path == uri.AbsolutePath)
            .FirstOrDefaultAsync();

        if (page == null)
        {
            page = new(uri, title);
            await pages.AddAsync(page);
        }

        if (page.Name != title)
            await pages.ExecuteAsync(page.Id, x => x.UpdateName(title));

        if (language != null && page.Language?.Id != language)
            await pages.ExecuteAsync(page.Id, x => x.Language = Language.Find(language));


        return page;
    }
}