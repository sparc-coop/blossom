using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Sparc.Blossom.Authentication;
using Stripe;
using System.Text;

namespace Sparc.Blossom.Content;

public class DocumentTranslator
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
            Doc.Dispose();
            Writer.Close();
            Reader.Close();
            Stream.Close();
        }
    }

    Document Open(string filePath)
    {
        var doc = WordprocessingDocument.Open(filePath, true).Clone();
        var body = doc.MainDocumentPart 
            ?? throw new InvalidOperationException("The document does not contain a main document part.");
        var stream = new MemoryStream();
        var reader = OpenXmlReader.Create(body);
        var writer = OpenXmlWriter.Create(stream);
        return new Document(doc, stream, reader, writer);
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

        return doc;
    }

    static Run CloneRun(Run run)
    {
        var newRun = new Run();
        if (run.RunProperties?.CloneNode(true) is RunProperties currentProperties)
            newRun.RunProperties = currentProperties;

        return newRun;
    }
    
    public List<TextContent> Extract(SparcDomain domain, string filePath, TranslationOptions? options = null)
    {
        var doc = Open(filePath);
        var content = new List<TextContent>();

        Language inputLanguage = Language.Find("en-US")!;
        doc.Writer.WriteStartDocument();
        Paragraph? currentParagraph = null;
        Run? currentRun = null;

        while (doc.Reader.Read())
        {
            if (doc.Reader.ElementType == typeof(Languages))
            {
                var languageVal = doc.Reader.Attributes.FirstOrDefault(x => x.LocalName == "val");
                if (languageVal != default)
                    inputLanguage = Language.Find(languageVal.Value) ?? inputLanguage;
            }

            else if (doc.Reader.ElementType == typeof(Paragraph))
            {
                if (doc.Reader.IsStartElement)
                {
                    currentParagraph = doc.Reader.LoadCurrentElement() as Paragraph;
                    doc.Writer.WriteStartElement(doc.Reader);
                }
                else
                {
                    currentParagraph = null;
                    doc.Writer.WriteEndElement();
                }
            }

            else if (doc.Reader.ElementType == typeof(Run))
            {
                if (doc.Reader.IsStartElement)
                    currentRun = doc.Reader.LoadCurrentElement() as Run;
                else
                    currentRun = null;
            }

            else if (doc.Reader.ElementType == typeof(Text))
            {
                if (doc.Reader.IsStartElement)
                {
                    var text = doc.Reader.GetText();
                    var textContent = new TextContent(domain.Domain, filePath, inputLanguage, text);
                    content.Add(textContent);

                    var id = new OpenXmlAttribute("id", "tovik", textContent.Id);
                    doc.Writer.WriteStartElement(doc.Reader, [id]);
                    doc.Writer.WriteString(text);
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
        doc.Doc.MainDocumentPart!.FeedData(doc.Stream);
        doc.SaveAndClose();

        return content;
    }

    public void Replace(SparcDomain domain, string filePath, List<TextContent> content)
    {
        var doc = Open(filePath);
        doc.Writer.WriteStartDocument();
        // TODO: Write language element

        while (doc.Reader.Read())
        {
            if (doc.Reader.ElementType == typeof(Text))
            {
                if (doc.Reader.IsStartElement)
                {
                    doc.Writer.WriteStartElement(doc.Reader);

                    var idAttr = doc.Reader.Attributes.FirstOrDefault(x => x.LocalName == "id" && x.NamespaceUri == "tovik");
                    if (idAttr != default)
                    {
                        var textContent = content.FirstOrDefault(c => c.Id == idAttr.Value);
                        if (textContent?.Text != null)
                            doc.Writer.WriteString(textContent.Text);
                        else
                            doc.Writer.WriteString(doc.Reader.GetText());
                    }
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
    }
}
