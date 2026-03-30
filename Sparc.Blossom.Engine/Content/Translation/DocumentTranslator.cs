using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using System.Text;
using Twilio.Rest;

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

    public async Task<Page> UploadAsync(SparcDomain domain, Stream stream, string filename)
    {
        var obfuscatedFileName = $"{domain.Id}/{Guid.NewGuid()}.docx";

        var doc = Open(stream);
        Simplify(doc);

        var file = new BlossomFile("documents", obfuscatedFileName, AccessTypes.Private, stream);
        await files.AddAsync(file);

        Page page = new(domain.Domain, obfuscatedFileName, filename)
        {
            Language = Language.Find("en-US")
        };
        await pages.AddAsync(page);

        await ExtractAsync(page.Domain, page.Id);

        return page;
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
                    newRun.AppendChild(new Text(mergedText.ToString()));
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

        var doc = Open(file.Stream!);

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

        var doc = OpenForStreamingEdit(file.Stream!);

        doc.Writer.WriteStartDocument();
        // TODO: Write language element

        while (doc.Reader.Read())
        {
            if (doc.Reader.ElementType == typeof(Text))
            {
                if (doc.Reader.IsStartElement)
                {
                    doc.Writer.WriteStartElement(doc.Reader);

                    var matchingContent = translatedContent.FirstOrDefault(x => x.OriginalText == doc.Reader.GetText());
                    if (matchingContent?.Text != null)
                        doc.Writer.WriteString(matchingContent.Text);
                    else
                        doc.Writer.WriteString(doc.Reader.GetText());
                }
                else
                {
                    doc.Writer.WriteEndElement();
                }
            }
            else
            {
                doc.CopyAsIs();
            }
        }

        doc.Stream.Position = 0;
        return doc.Stream;
    }

    Document OpenForStreamingEdit(Stream stream)
    {
        var doc = Open(stream);
        var memoryStream = new MemoryStream();
        var reader = OpenXmlReader.Create(doc.MainDocumentPart!);
        var writer = OpenXmlWriter.Create(stream);
        return new Document(doc, memoryStream, reader, writer);
    }

    WordprocessingDocument Open(Stream stream)
    {
        var doc = WordprocessingDocument.Open(stream, true).Clone();
        if (doc.MainDocumentPart == null)
            throw new InvalidOperationException("The document does not contain a main document part.");

        return doc;
    }

}
