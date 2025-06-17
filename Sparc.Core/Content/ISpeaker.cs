namespace Sparc.Engine;

public interface ISpeaker
{
    Task<AudioContent?> SpeakAsync(TextContent message, string? voiceId = null);
    Task<List<Voice>> GetVoicesAsync(Language? language = null, string? dialect = null, string? gender = null);
    Task<AudioContent> SpeakAsync(List<TextContent> messages);
    Task<string?> GetClosestVoiceAsync(Language language, string? gender, string deterministicId);
}