namespace Kori;

public class Contents(BlossomAggregateOptions<Content> options, IRepository<Page> pages, KoriTranslatorProvider translator) : BlossomAggregate<Content>(options)
{
    public BlossomQuery<Content> Search(string searchTerm) => Query().Where(content =>
         ((content.Text != null && content.Text.ToLower().Contains(searchTerm) == true) ||
         (content.OriginalText != null && content.OriginalText.ToLower().Contains(searchTerm) == true) ||
         (content.Domain != null && content.Domain.ToLower().Contains(searchTerm) == true) ||
         (content.Path != null && content.Path.ToLower().Contains(searchTerm) == true)));

    public async Task<IEnumerable<Content>> GetAll(string pageId, string? fallbackLanguageId = null)
    {
        var language = User.Language(fallbackLanguageId);
        var page = await pages.FindAsync(pageId);
        var content = await page.LoadContentAsync(language, Repository, translator);
        return content;
    }

    public BlossomQuery<Content> All(string pageId) => Query().Where(content => content.PageId == pageId && content.SourceContentId == null);

    public async Task<IEnumerable<Language>> Languages()
        => await translator.GetLanguagesAsync();
}
