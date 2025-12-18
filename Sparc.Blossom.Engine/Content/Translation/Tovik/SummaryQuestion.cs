using Sparc.Blossom.Spaces;

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

    public SummaryQuestion(BlossomSpace space, IEnumerable<BlossomSpace> otherSpaces) : base(
    "Given the given descriptions of other rooms in this space, refine this room summary to be as far apart as possible from all the other rooms, so that it is more obvious what this room is about.")
    {
        Instructions = "You are an assistant that summarizes messages into a brief overview that will be used for room identification.\r\n" +
            "Analyze the provided messages and extract the main themes to create a concise summary.\r\n\r\n" +
            "The summary should include:\r\n" +
            "- Name: A short, descriptive room title for the collection of messages.\r\n" +
            "- Topic: The 10-20 word primary subject matter discussed in the messages.\r\n" +
            "- Description: A 50-100 word overview highlighting the key points and themes.";

        Text += "The following are descriptions of other rooms in this space:\r\n";
        foreach (var otherSpace in otherSpaces)
            Text += "\r\n- " + otherSpace.Description!.Replace('\u00A0', ' ');

        Text += "\r\n\r\nThis Room: ";
        Text += "\r\n- " + space.Description!.Replace('\u00A0', ' ');
    }
}
