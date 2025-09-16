using Sparc.Blossom;
using Sparc.Blossom.Authentication;
using Sparc.Core;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Sparc.Blossom.Content;

public record EditHistory(DateTime Timestamp, string Text);
public record TovikContentTranslated(TextContent Content, int TokenCount, decimal? Cost = null, string? Description = null, string? Response = null) : BlossomEvent(Content);

public record ContentTranslation(string Id, Language Language, string? SourceContentId = null)
{
    public ContentTranslation(string id) : this(id, new()) { }
}

public class TextContent : BlossomEntity<string>
{
    public string Domain { get; set; } = null!;
    public string Path { get; set; } = "";
    public string? SourceContentId { get; set; }
    public string LanguageId { get; set; } = null!;
    public Language Language { get; set; } = null!;
    public string ContentType { get; set; } = "Text";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public DateTime? LastModified { get; set; }
    public DateTime? DeletedDate { get; set; }
    public BlossomAvatar? User { get; set; }
    public AudioContent? Audio { get; set; }
    public string? Type { get; set; }
    public string? Text { get; set; }
    public List<ContentTranslation> Translations { get; set; } = [];
    internal long Charge { get; set; }
    internal decimal Cost { get; set; }
    public string OriginalText { get; set; } = "";
    internal List<EditHistory> EditHistory { get; set; } = [];
    public string Html { get; set; } = "";

    [JsonConstructor]
    private TextContent()
    { }

    public TextContent(string domain, string pageId, string languageId)
    {
        Id = Guid.NewGuid().ToString();
        Domain = domain;
        Path = pageId;
        User = new BlossomUser().Avatar;
        Language = new(languageId);
        LanguageId = Language.Id;
    }

    public TextContent(string domain, string pageId, Language language, string text, BlossomUser? user = null, string? originalText = null, string contentType = "Text")
        : this(domain, pageId, language.Id)
    {
        User = user?.Avatar;
        Language = user?.Avatar.Language ?? language;
        Audio = user?.Avatar.Language?.VoiceId == null ? null : new(null, 0, user.Avatar.Language.VoiceId);
        OriginalText = originalText ?? "";
        ContentType = contentType;
        SetText(text);
    }

    public TextContent(TextContent sourceContent, Language toLanguage, string text) : this(sourceContent.Domain, sourceContent.Path, toLanguage.Id)
    {
        Id = sourceContent.Id; // this hash is coming from the client, so we use the source content's ID
        SourceContentId = sourceContent.Id;
        User = sourceContent.User;
        Audio = sourceContent.Audio?.Voice == null ? null : new(null, 0, sourceContent.Audio.Voice);
        Language = toLanguage;
        Timestamp = sourceContent.Timestamp;
        OriginalText = sourceContent.Text ?? "";
        SetText(text);
    }

    //public static string IdHash(string? text, Language language) => BlossomHash.MD5($"{text}:{language}");

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
        if (voiceId == null && (Audio?.Voice == null || !Audio.Voice.StartsWith(Language.LanguageId)))
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

    private int WordCount()
    {
        var textToCount = string.IsNullOrWhiteSpace(OriginalText) ? Text : OriginalText;

        if (string.IsNullOrWhiteSpace(textToCount))
            return 0;

        var matches = Regex.Matches(textToCount, @"[\p{L}\p{N}]+", RegexOptions.Multiline);
        return matches.Count;
    }

    public TextContent AddCharge(int numTokens, decimal costPerToken, string? description = null)
    {
        Charge += numTokens;
        Cost -= numTokens * costPerToken;

        if (numTokens > 0 || Charge > 0)
            Broadcast(new TovikContentTranslated(this, numTokens, numTokens * costPerToken, description));

        return this;
    }

    public void AddCharge(decimal? costPerWord = null, string? description = null, string? response = null)
    {
        var wordCount = WordCount();
        if (wordCount > 0)
        {
            Charge += wordCount;
        }
        
        var cost = costPerWord.HasValue ? costPerWord.Value * wordCount : 0;
        if (cost > 0)
            Cost -= cost;

        if (cost > 0 || Charge > 0)
            Broadcast(new TovikContentTranslated(this, wordCount, cost, description, response));
    }

    internal void Delete()
    {
        DeletedDate = DateTime.UtcNow;
    }

    public T? Cast<T>()
    {
        try
        {
            return JsonSerializer.Deserialize<T>(Text!);
        }
        catch
        {
            return default;
        }
    }

    public TextContent SetText(string text)
    {
        if (Text == text)
            return this;

        if (!string.IsNullOrWhiteSpace(Text))
            EditHistory.Add(new(LastModified ?? Timestamp, Text!));

        if (string.IsNullOrWhiteSpace(OriginalText))
            OriginalText = text;

        Text = text;
        LastModified = DateTime.UtcNow;

        return this;
    }


}
