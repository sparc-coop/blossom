using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Sparc.Blossom.Data;
using System.Text;

namespace Sparc.Blossom.Content;

public class DocumentTranslator(IRepository<BlossomFile> files, IRepository<Page> pages, IRepository<TextContent> content)
{
    record Document(WordprocessingDocument Doc, MemoryStream Stream, OpenXmlReader Reader, OpenXmlWriter Writer)
    {
        public void CopyAsIs()
        {
            if (Reader.IsStartElement)
                Writer.WriteStartElement(Reader);
            else
                Writer.WriteEndElement();
        }

        public void SaveAndClose()
        {
            Doc.MainDocumentPart!.Document!.Save();
            Doc.Dispose();
            Writer.Close();
            Reader.Close();
            Stream.Close();
        }
    }

    public WordprocessingDocument Simplify(WordprocessingDocument doc)
    {
        var paragraphs = doc.MainDocumentPart?.Document?.Body?.Elements<Paragraph>().ToList();
        if (paragraphs == null)
            return doc;

        foreach (var paragraph in paragraphs)
        {
            var runs = paragraph.Elements<Run>().ToList();
            if (runs.Count <= 1)
                continue;

            var newRun = CloneRun(runs[0]);

            var mergedText = new StringBuilder();
            foreach (var run in runs)
            {
                // Append text until the run's properties change
                if (run.RunProperties?.OuterXml != newRun.RunProperties?.OuterXml)
                {
                    var text = mergedText.ToString();
                    var preserveWhitespace = text.First() == ' ' || text.Last() == ' ';
                    newRun.AppendChild(new Text(text) { Space = preserveWhitespace ? SpaceProcessingModeValues.Preserve : null });
                    paragraph.AppendChild(newRun);
                    mergedText.Clear();

                    // Start a new run with the new properties
                    newRun = CloneRun(run);
                }

                foreach (var text in run.Elements<Text>())
                {
                    if (text.Space != null && text.Space == SpaceProcessingModeValues.Preserve)
                        mergedText.Append(text.Text);
                    else
                        mergedText.Append(text.Text.Trim());
                }

                paragraph.RemoveChild(run);
            }

            newRun.AppendChild(new Text(mergedText.ToString()));
            paragraph.AppendChild(newRun);
        }

        doc.MainDocumentPart!.Document!.Save();
        return doc;
    }

    static Run CloneRun(Run run)
    {
        var newRun = new Run();
        if (run.RunProperties?.CloneNode(true) is RunProperties currentProperties)
            newRun.RunProperties = currentProperties;

        return newRun;
    }

    public async Task<(Page page, List<TextContent> content)> ExtractAsync(string domain, string id, bool forceReload = false)
    {
        var page = await pages.Query.Where(x => x.Domain == domain && x.Id == id).FirstOrDefaultAsync()
               ?? throw new Exception("Document not found");

        var textContent = await content.Query
            .Where(x => x.Domain == domain && x.SpaceId == id && x.LanguageId == page.Language!.Id)
            .ToListAsync();

        if (textContent.Count > 0 && !forceReload)
            return (page, textContent);

        var file = await files.FindAsync($"documents/{page.Path}")
            ?? throw new Exception($"File not found for page {page.Path}");

        var (doc, stream) = Open(file.Stream!);

        var text = doc.MainDocumentPart?.Document?.Body?
            .Descendants<Text>()
            .Select(x => new TextContent(page, x.Text))
            .ToList() ?? [];

        await content.UpdateAsync(text);

        return (page, text);
    }

    public async Task<Stream> ReplaceAsync(Page page, List<TextContent> translatedContent)
    {
        var file = await files.FindAsync($"documents/{page.Path}")
            ?? throw new Exception($"File not found for page {page.Path}");

        var (doc, stream) = Open(file.Stream!);
        var text = doc.MainDocumentPart?.Document?.Body?
            .Descendants<Text>()
            .ToList() ?? [];

        foreach (var item in text)
        {
            var originalText = item.Text;
            var matchingContent = translatedContent.FirstOrDefault(x => x.OriginalText == originalText);
            if (matchingContent?.Text != null)
            {
                var preWhitespace = item.Space?.Value == SpaceProcessingModeValues.Preserve ? item.Text.Length - item.Text.TrimStart(' ').Length : 0;
                var postWhitespace = item.Space?.Value == SpaceProcessingModeValues.Preserve ? item.Text.Length - item.Text.TrimEnd(' ').Length : 0;

                item.Text = new string(' ', preWhitespace) + matchingContent.Text.Trim() + new string(' ', postWhitespace);
            }
        }

        doc.MainDocumentPart!.Document!.Save();
        doc.Dispose();

        stream!.Position = 0;
        return stream!;
    }

    (WordprocessingDocument, MemoryStream) Open(Stream stream)
    {
        var newStream = new MemoryStream();
        var doc = WordprocessingDocument.Open(stream, true).Clone(newStream);
        if (doc.MainDocumentPart == null)
            throw new InvalidOperationException("The document does not contain a main document part.");

        Simplify(doc);
        return (doc, newStream);
    }
}
