using Sparc.Blossom.Authentication;
using Sparc.Blossom.Content;

namespace Sparc.Blossom.Realtime;

public class TextMessage : TextContent
{
    public string RoomId { get; set; } = null!;

    public TextMessage(string domain, string roomId, string languageId) : base(domain, languageId)
    {
        Id = Guid.NewGuid().ToString();
        RoomId = roomId;
        LanguageId = languageId;
        Language = new(languageId);
        User = new BlossomUser().Avatar;
    }

    public TextMessage(string domain, string roomId, Language language, string text, BlossomUser? user = null, string? originalText = null)
        : this(domain, roomId, language.Id)
    {
        User = user?.Avatar;
        Language = user?.Avatar.Language ?? language;
        Audio = user?.Avatar.Language?.VoiceId == null ? null : new(null, 0, user.Avatar.Language.VoiceId);
        OriginalText = originalText ?? "";
        ContentType = "Text";
        SetText(text);
    }

    public TextMessage(TextMessage sourceMessage, Language toLanguage, string text) : this(sourceMessage.Domain, sourceMessage.RoomId, toLanguage.Id)
    {
        SourceContentId = sourceMessage.Id;
        User = sourceMessage.User;
        Audio = sourceMessage.Audio?.Voice == null ? null : new(null, 0, sourceMessage.Audio.Voice);
        Language = toLanguage;
        Timestamp = sourceMessage.Timestamp;
        OriginalText = sourceMessage.Text ?? "";
        SetText(text);
    }

}
