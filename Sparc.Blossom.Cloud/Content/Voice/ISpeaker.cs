namespace Kori;

internal interface ISpeaker
{
    Task<AudioContent?> SpeakAsync(Content message, string? voiceId = null);
    Task<List<Voice>> GetVoicesAsync(Language? language = null, string? dialect = null, string? gender = null);
    Task<AudioContent> SpeakAsync(List<Content> messages);
    Task<string?> GetClosestVoiceAsync(Language language, string? gender, string deterministicId);
}