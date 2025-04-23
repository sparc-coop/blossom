using DeepL.Model;
using Markdig;
using Markdig.Renderers;

namespace Kori;

public record EditHistory(DateTime Timestamp, string Text);
public record AudioContent(string? Url, long Duration, string Voice)
{
    public List<Word> Words { get; set; } = [];
}

public record ContentTranslation(string Id, Language Language, string? SourceContentId = null)
{
    public ContentTranslation(string id) : this(id, new()) { }
}

    public class Content : BlossomEntity<string>
{
    public string Domain { get; private set; }
    public string Path { get; private set; }
    public string? SourceContentId { get; private set; }
    public Language Language { get; protected set; }
    public string ContentType { get; private set; }
    public DateTime Timestamp { get; private set; }
    public DateTime? LastModified { get; private set; }
    public DateTime? DeletedDate { get; private set; }
    public UserAvatar? User { get; private set; }
    public AudioContent? Audio { get; private set; }
    public string? Text { get; private set; }
    public List<ContentTranslation> Translations { get; private set; }
    internal long Charge { get; private set; }
    internal decimal Cost { get; private set; }
    public string OriginalText { get; set; }
    internal List<EditHistory> EditHistory { get; private set; }
    public string Html { get; set; }
    public string PageId { get; internal set; }

    protected Content(string pageId)
    {
        Id = Guid.NewGuid().ToString();
        PageId = pageId;
        Domain = new Uri(pageId).Host;
        Path = new Uri(pageId).AbsolutePath;
        User = new KoriUser().Avatar;
        Language = new();
        Translations = [];
        EditHistory = [];
        Html = string.Empty;
        OriginalText = string.Empty;
        ContentType = "Text";
    }

    public Content(string pageId, Language language, string text, KoriUser? user = null, string? originalText = null, string contentType = "Text") 
        : this(pageId)
    {
        User = user?.Avatar;
        Language = user?.Avatar.Language ?? language;
        Audio = user?.Avatar.Language?.VoiceId == null ? null : new(null, 0, user.Avatar.Language.VoiceId);
        Timestamp = DateTime.UtcNow;
        OriginalText = originalText ?? "";
        ContentType = contentType;
        SetTextAndHtml(text);
    }

    internal Content(Content sourceContent, Language toLanguage, string text) : this(sourceContent.PageId)
    {
        SourceContentId = sourceContent.Id;
        User = sourceContent.User;
        Audio = sourceContent.Audio?.Voice == null ? null : new(null, 0, new(sourceContent.Audio.Voice));
        Language = toLanguage;
        Timestamp = sourceContent.Timestamp;
        OriginalText = sourceContent.OriginalText;
        SetTextAndHtml(text);
    }

    internal async Task<Content?> TranslateAsync(Language language, IRepository<Content> contents, KoriTranslatorProvider provider)
    {
        if (Language == language)
            return this;

        var translation = Translations.FirstOrDefault(x => x.Language == language);

        if (translation != null)
            return await contents.FindAsync(translation.Id);

        var translator = await provider.For(Language, language);
        if (translator == null)
            return null;

        var translatedContent = await translator.TranslateAsync(this, language);

        if (translatedContent != null)
            AddTranslation(translatedContent);

        return translatedContent;
    }

    internal async Task<AudioContent?> SpeakAsync(ISpeaker engine, string? voiceId = null)
    {
        if (voiceId == null && (Audio?.Voice == null || !Audio.Voice.StartsWith(Language.Id)))
        {
            voiceId = await engine.GetClosestVoiceAsync(Language, User?.Gender, User?.Id ?? Guid.NewGuid().ToString());
        }

        var audio = await engine.SpeakAsync(this, voiceId);

        if (audio != null)
        {
            Audio = audio;
        }

        return audio;
    }

    internal bool HasTranslation(Language language)
    {
        return Language.Equals(language)
            || Translations != null && Translations.Any(x => x.Language.Equals(language));
    }

    internal void AddTranslation(Content translatedContent)
    {
        if (HasTranslation(translatedContent.Language))
        {
            // Set the newly translated content's ID to the existing translation so that it is updated in the repository
            var translation = Translations.FirstOrDefault(x => x.Language == translatedContent.Language);
            if (translation?.SourceContentId != null)
                translatedContent.Id = translation.SourceContentId;
        }
        else
        {
            Translations.Add(new(translatedContent.Id, translatedContent.Language));
        }
    }

    internal void AddCharge(long ticks, decimal cost, string description)
    {
        Charge += ticks;
        Cost -= cost;
        //if (ticks > 0)
        //Broadcast(new CostIncurred(this, description, ticks));
    }

    internal void Delete()
    {
        DeletedDate = DateTime.UtcNow;
    }

    public void SetHtmlFromMarkdown()
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

        renderer.Render(Markdown.Parse(Text ?? string.Empty, pipeline));
        writer.Flush();

        Html = writer.ToString();
    }

    public Content SetTextAndHtml(string text)
    {
        if (Text == text)
            return this;

        if (!string.IsNullOrWhiteSpace(Text))
            EditHistory.Add(new(LastModified ?? Timestamp, Text));

        if (string.IsNullOrWhiteSpace(OriginalText))
            OriginalText = text;

        Text = text;
        LastModified = DateTime.UtcNow;

        SetHtmlFromMarkdown();

        return this;
    }


}
