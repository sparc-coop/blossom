
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Content;

public class Pages(BlossomAggregateOptions<Page> options) : BlossomAggregate<Page>(options), IBlossomEndpoints
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

    private async Task Visit(Visit visit, Language language)
    {
        var page = await Repository.Query
            .Where(x => x.Domain == visit.Domain && x.Path == visit.Path)
            .FirstOrDefaultAsync();

        if (page == null)
            return;

        page.RegisterVisit(language);
        await Repository.UpdateAsync(page);
    }

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("pages").RequireCors("Tovik");
        group.MapPost("visit", async (Pages pages, HttpRequest request, Visit visit) =>
        {
            var language = Language.Find(request.Headers.AcceptLanguage);
            await pages.Visit(visit, language!);
            return Results.Ok();
        });

    }
}