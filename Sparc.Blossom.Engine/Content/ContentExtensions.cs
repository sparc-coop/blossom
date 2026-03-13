using Markdig;
using Markdig.Renderers;

namespace Sparc.Blossom.Content;

internal static class ContentExtensions
{
    public static void SetHtmlFromMarkdown(this TextContent content)
    {
        var pipeline = new MarkdownPipelineBuilder()
            .UseEmphasisExtras()
            .Build();

        var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer)
        {
            ImplicitParagraph = true //This is needed to render a single line of text without a paragraph tag
        };
        pipeline.Setup(renderer);

        renderer.Render(Markdown.Parse(content.Text ?? string.Empty, pipeline));
        writer.Flush();

        content.Html = writer.ToString();
    }

    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items,                                                        int maxItems)
    {
        return items.Select((item, inx) => new { item, inx })
                    .GroupBy(x => x.inx / maxItems)
                    .Select(g => g.Select(x => x.item));
    }
}
