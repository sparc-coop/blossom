using Sparc.Blossom;
using Sparc.Blossom.Authentication;
using System.Text.Json.Serialization;

namespace Sparc.Engine;

public record EditHistory(DateTime Timestamp, string Text);

public record ContentTranslation(string Id, Language Language, string? SourceContentId = null)
{
    public ContentTranslation(string id) : this(id, new()) { }
}

public class TextContent : BlossomEntity<string>
{
    public string Domain { get; private set; }
    public string? Path { get; private set; }
    public string? SourceContentId { get; private set; }
    public string LanguageId { get; private set; }
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
    public string? PageId { get; internal set; }

    [JsonConstructor]
    public TextContent(string domain, string languageId)
    {
        Id = Guid.NewGuid().ToString();
        LanguageId = languageId;
        Domain = domain;
        User = new BlossomUser().Avatar;
        Language = new();
        Translations = [];
        EditHistory = [];
        Html = string.Empty;
        OriginalText = string.Empty;
        ContentType = "Text";
    }

    public TextContent(string domain, Language language, string text, BlossomUser? user = null, string? originalText = null, string contentType = "Text")
        : this(domain, language.Id)
    {
        User = user?.Avatar;
        Language = user?.Avatar.Language ?? language;
        Audio = user?.Avatar.Language?.VoiceId == null ? null : new(null, 0, user.Avatar.Language.VoiceId);
        Timestamp = DateTime.UtcNow;
        OriginalText = originalText ?? "";
        ContentType = contentType;
        SetText(text);
    }

    public TextContent(TextContent sourceContent, Language toLanguage, string text) : this(sourceContent.Domain, sourceContent.LanguageId)
    {
        SourceContentId = sourceContent.Id;
        User = sourceContent.User;
        Audio = sourceContent.Audio?.Voice == null ? null : new(null, 0, sourceContent.Audio.Voice);
        Language = toLanguage;
        Timestamp = sourceContent.Timestamp;
        OriginalText = sourceContent.OriginalText;
        SetText(text);
    }

    //internal async Task<TextContent?> TranslateAsync(Language language, IRepository<TextContent> contents, BlossomTranslator provider)
    //{
    //    if (Language == language)
    //        return this;

    //    var translation = Translations.FirstOrDefault(x => x.Language == language);

    //    if (translation != null)
    //        return await contents.FindAsync(translation.Id);

    //    var translator = await provider.For(Language, language);
    //    if (translator == null)
    //        return null;

    //    var translatedContent = await translator.TranslateAsync(this, language);

    //    if (translatedContent != null)
    //        AddTranslation(translatedContent);

    //    return translatedContent;
    //}

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

    internal void AddTranslation(TextContent translatedContent)
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

    public void AddCharge(long ticks, decimal cost, string description)
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


    public TextContent SetText(string text)
    {
        if (Text == text)
            return this;

        if (!string.IsNullOrWhiteSpace(Text))
            EditHistory.Add(new(LastModified ?? Timestamp, Text));

        if (string.IsNullOrWhiteSpace(OriginalText))
            OriginalText = text;

        Text = text;
        LastModified = DateTime.UtcNow;

        return this;
    }


}
