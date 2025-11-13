using System.Text.Json;
using System.Text.Json.Nodes;

namespace Sparc.Blossom.Content.OpenAI;
internal class OpenAIAnswer()
{
    public string Name { get; set; } = string.Empty;
    public string? Text { get; set; }
    public string? Error { get; set; }
    public int TokensUsed { get; set; }
    public object? GenericTypedAnswer { get; internal set; }
    public dynamic? Value { get; set; }
    internal string? ResponseId { get; private set; }
    bool IsExpanded;
    bool HasAnswer;

    public virtual void SetResponse(string responseId, string response, int tokensUsed)
    {
        ResponseId = responseId;
        TokensUsed = tokensUsed;
        Text = response;
        Error = null;

        if (string.IsNullOrWhiteSpace(Name))
        {
            GenericTypedAnswer = response;
            IsExpanded = true;
        }
        else if (this[Name] != null || HasAnswer)
        {
            Text = this[Name]?.ToString();
            IsExpanded = true;
            GenericTypedAnswer = this[Name];
        }

        try
        {

            var unwrapped = this[Name]?.ToString();
            if (unwrapped != null)
                Value = JsonSerializer.Deserialize<dynamic>(unwrapped);
            else if (unwrapped == null)
            {
                try
                {
                    Value = JsonSerializer.Deserialize<dynamic>(response);
                }
                catch (JsonException)
                {
                }
            }
        }
        catch { }

        Log("Info", $"Answer set to {response} via {responseId}. {tokensUsed} tokens used.");
    }

    public void SetError(string? error, int? tokensUsed = null)
    {
        Error = error;

        if (error != null)
            Log("Error", error);

        if (tokensUsed != null)
            TokensUsed = tokensUsed.Value;
    }

    public object? this[string propertyName]
    {
        get
        {
            if (IsExpanded)
                return Text;

            if (Text == null)
                return null;

            var json = JsonNode.Parse(Text);
            if (json == null)
                return null;
            if (json is JsonObject jsonObject && jsonObject.ContainsKey(propertyName))
            {
                HasAnswer = true;
                return jsonObject[propertyName];
            }

            return null;
        }
    }

    public void Log(string type, string message)
    {
        Console.WriteLine($"{DateTime.UtcNow:O} [{type}] {message}");
        //Logs.Add(new UnlimitedLog(DateTime.UtcNow, type, message));
    }
}