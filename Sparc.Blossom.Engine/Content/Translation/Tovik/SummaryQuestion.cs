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
            "- Name: A short, 2 to 3 word descriptive room title for the collection of messages. This name should be extremely specific to the primary subject matter of the room. All lower case, words hyphenated.\r\n" +
            "- Topic: The 10-20 word primary subject matter discussed in the messages.\r\n" +
            "- Description: A 50-100 word overview highlighting the key points and themes.";

        Text += "\r\n\r\nMessages: ";
        AddMessages(messages, tokenLimit);
    }

    public SummaryQuestion(IEnumerable<TextContent> leftMessages, IEnumerable<TextContent> rightMessages, int tokenLimit) 
        : base(
    "Provide a concise summary of the following messages, including a name, topic, and description, and a constrasting set of topics between the left and right set of messages." +
    "The summary should capture the main themes and key points discussed in the messages.")
    {
        Instructions = "You are an assistant that summarizes two sets of messages into a new semantic facet that will be used for room facets identification.\r\n" +
                    "Analyze the provided messages and extract the main themes to create a concise summary.\r\n\r\n" +
                    "The summary should include:\r\n" +
                    "- Name: A short, 2 to 3 word descriptive facet title encompassing both sets of messages. This name should be extremely specific to the primary subject matter of the facet. All lower case, words hyphenated.\r\n" +
                    "- Topic: The 10-20 word primary subject matter discussed in both sets of messages.\r\n" +
                    "- Description: A 50-100 word overview highlighting the key points and themes shared by both sets of messages.";

        Instructions += "- LeftTopic: A short, 2 to 3 word descriptive topic for the left set of messages that distinguishes it from the right set of messages.\r\n";
        Instructions += "- RightTopic: A short, 2 to 3 word descriptive topic for the right set of messages that distinguishes it from the left set of messages.";

        Text += "\r\n\r\nLeft Messages: ";
        AddMessages(leftMessages, tokenLimit / 2);

        Text += "\r\n\r\nRight Messages: ";
        AddMessages(rightMessages, tokenLimit / 2);
    }

    private void AddMessages(IEnumerable<TextContent> messages, int tokenLimit)
    {
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
            "- Name: A short, 2 to 3 word descriptive room title for the collection of messages. This name should be extremely specific to the primary subject matter of the room. All lower case, words hyphenated.\r\n" +
            "- Topic: The 10-20 word primary subject matter discussed in the messages.\r\n" +
            "- Description: A 50-100 word overview highlighting the key points and themes.";

        Text += "The following are descriptions of other rooms in this space:\r\n";
        foreach (var otherSpace in otherSpaces)
            Text += "\r\n- " + otherSpace.Description!.Replace('\u00A0', ' ');

        Text += "\r\n\r\nThis Room: ";
        Text += "\r\n- " + space.Description!.Replace('\u00A0', ' ');
    }
}
