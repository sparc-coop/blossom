namespace Sparc.Blossom.Content;

internal class SummaryQuestion : BlossomQuestion<BlossomSummary>
{

    public SummaryQuestion(IEnumerable<TextContent> messages, int tokenLimit) : base(
    "Provide a concise summary of the following messages, including a name, topic, and description. " +
    "The summary should capture the main themes and key points discussed in the messages.")
    {
        Instructions = "You are an assistant that summarizes messages into a brief overview that will be used for room identification.\r\n" +
            "Analyze the provided messages and extract the main themes to create a concise summary.\r\n\r\n" +
            "The summary should include:\r\n" +
            "- Name: A short, descriptive room title for the collection of messages.\r\n" +
            "- Topic: The 10-20 word primary subject matter discussed in the messages.\r\n" +
            "- Description: A 50-100 word overview highlighting the key points and themes.";

        Text += "\r\n\r\nMessages: ";
        foreach (var message in messages)
        {
            if (Text.Length + message.Text!.Length > tokenLimit * 4 * 0.8)
            {
                Text += "\r\n- [Truncated additional messages due to token limit]";
                break;
            }
            Text += "\r\n- " + message.Text!.Replace('\u00A0', ' ');
        }
    }
}
