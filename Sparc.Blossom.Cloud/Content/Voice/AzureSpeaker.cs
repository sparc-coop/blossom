using Microsoft.CognitiveServices.Speech;
using NAudio.Lame;
using NAudio.Wave;

namespace Kori;

internal class AzureSpeaker : ISpeaker
{
    readonly HttpClient Client;
    readonly string SubscriptionKey;

    internal IFileRepository<BlossomFile> Files { get; }
    internal static List<Voice>? Voices;

    public AzureSpeaker(IConfiguration configuration, IFileRepository<BlossomFile> files)
    {
        SubscriptionKey = configuration.GetConnectionString("Cognitive")!;

        Client = new HttpClient
        {
            BaseAddress = new Uri("	https://southcentralus.tts.speech.microsoft.com")
        };
        Client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);

        Files = files;
    }

    public async Task<AudioContent?> SpeakAsync(Content message, string? voiceId = null)
    {
        if (voiceId == null && message.Audio?.Voice == null)
            return null;

        var synthesizer = Synthesizer(voiceId ?? message.Audio!.Voice);
        var words = new List<Word>();
        synthesizer.WordBoundary += (sender, e) =>
        {
            words.Add(new((long)e.AudioOffset / 10000, (long)e.Duration.TotalMilliseconds, e.Text));
        };

        var result = await synthesizer.SpeakTextAsync(message.Text);
        if (result.AudioDuration == TimeSpan.Zero)
            return null;

        using var stream = new MemoryStream(ConvertWavToMp3(result.AudioData), false);
        BlossomFile file = new("speak", $"{message.Id}/{result.ResultId}.mp3", AccessTypes.Public, stream);
        await Files.AddAsync(file);

        var cost = message.Text!.Length / 1_000_000M * 16.00M; // $16 per 1M characters
        var ticks = result.AudioDuration.Ticks;
        message.AddCharge(ticks, cost, $"Speak message in voice {message.Audio!.Voice}");
        
        return new(file.Url!, (long)result.AudioDuration.TotalMilliseconds, message.Audio!.Voice) { Words = words };
    }

    public async Task<AudioContent> SpeakAsync(List<Content> messages)
    {
        byte[] buffer = new byte[1024];
        WaveFileWriter? waveFileWriter = null;
        using var combinedAudio = new MemoryStream();

        foreach (var message in messages.Where(x => x.Audio?.Url != null).OrderBy(x => x.Timestamp))
        {
            HttpClient client = new();
            var inputAudio = await client.GetStreamAsync(message.Audio!.Url);

            using WaveFileReader reader = new(inputAudio);
            if (waveFileWriter == null)
            {
                // first time in create new Writer
                waveFileWriter = new WaveFileWriter(combinedAudio, reader.WaveFormat);
            }
            else if (!reader.WaveFormat.Equals(waveFileWriter.WaveFormat))
            {
                throw new InvalidOperationException("Can't concatenate WAV Files that don't share the same format");
            }

            int read;
            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                waveFileWriter.Write(buffer, 0, read);
            }
        }

        waveFileWriter?.Dispose();

        BlossomFile file = new("speak", $"{Guid.NewGuid()}.wav", AccessTypes.Public, combinedAudio);
        await Files.AddAsync(file);

        return new(file.Url, 0, messages.First().Audio!.Voice);
    }

    public async Task<List<Voice>> GetVoicesAsync(Language? language = null, string? dialect = null, string? gender = null)
    {
        Voices ??= await Client.GetFromJsonAsync<List<Voice>>("/cognitiveservices/voices/list");

        return Voices!
            .Where(x => language == null || x.Locale.StartsWith(language.Id))
            .Where(x => dialect == null || x.Locale.Split("-").Last() == dialect)
            .Where(x => gender == null || x.Gender == gender)
            .ToList();
    }

    SpeechSynthesizer Synthesizer(string voice)
    {
        var config = SpeechConfig.FromSubscription(SubscriptionKey, "southcentralus");
        config.SpeechSynthesisVoiceName = voice;
        //config.SetProperty(PropertyId.Speech_LogFilename, "speechlog.txt");

        return new SpeechSynthesizer(config, null);
    }

    internal static byte[] ConvertWavToMp3(byte[] wavFile)
    {
        using var retMs = new MemoryStream();
        using var ms = new MemoryStream(wavFile);
        using var rdr = new WaveFileReader(ms);
        using var wtr = new LameMP3FileWriter(retMs, rdr.WaveFormat, 128);
        rdr.CopyTo(wtr);
        wtr.Flush();
        return retMs.ToArray();
    }

    public async Task<string?> GetClosestVoiceAsync(Language language, string? gender, string deterministicId)
    {
        var voices = await GetVoicesAsync(language, null, gender);
        var hash = deterministicId.ToCharArray().Aggregate(0, (acc, c) => acc + c);
        return voices[hash % voices.Count].Name;
    }
}
