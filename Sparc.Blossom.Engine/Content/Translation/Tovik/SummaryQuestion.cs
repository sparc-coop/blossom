using Sparc.Blossom.Spaces;

namespace Sparc.Blossom.Content;

internal class SummaryQuestion : BlossomQuestion<BlossomSummary>
{

    public SummaryQuestion(IEnumerable<Post> messages, int tokenLimit) : base(
    "Provide a concise summary of the following messages, including a name, topic, and description. " +
    "The summary should capture the main themes and key points discussed in the messages.")
    {
        Instructions = "You are an assistant that summarizes messages into a brief overview that will be used for room identification.\r\n" +
            "Analyze the provided messages and extract the main themes to create a concise summary.\r\n\r\n" +
            "The summary should include:\r\n" +
            "- Name: A short, 2 to 3 word descriptive room title for the collection of messages. This name should be extremely specific to the primary subject matter of the room.\r\n" +
            "- Topic: The 10-20 word primary subject matter discussed in the messages.\r\n" +
            "- Description: A 50-100 word overview highlighting the key points and themes.";

        Text += "\r\n\r\nMessages: ";
        AddMessages(messages, tokenLimit);
    }

    public SummaryQuestion(Facet facet, BlossomVector answerVector)
        : base(
    "Given the following sequential left-to-right list of representative messages on this axis, provide a name, topic, and description of the center point of the axis, plus descriptions of the left and right sides of the axis." +
    " This will be used for a game quest description, so phrase accordingly.")
    {
        Instructions = "You are an assistant that summarizes two sets of messages into a new semantic facet that will be used for a game quest.\r\n" +
                    "Analyze the provided messages and extract the main themes from the messages to guide the user from the left side to the right side of the quest, via the conflict or tension located within.\r\n\r\n" +
                    "Use the given weight of each message, which is on a scale of 0 to 1, 1 being highest, to prioritize more relevant messages in the summary.\r\n" +
                    "The summary should include:\r\n" +
                    "- Name: A short, 2 to 3 word descriptive quest title representing the journey to the center point of the axis. This name should be extremely specific to the primary subject matter of the quest.\r\n" +
                    "- Topic: The 10-20 word primary subject matter represented by the center point of the axis. This should be phrased as a game quest description.\r\n" +
                    "- Description: A 10-20 word set of hints as to how to navigate closer to the center point of the axis.";

        Instructions += "- LeftTopic: A short, 2 to 3 word descriptive topic for the left set of messages that distinguishes it from the right set of messages and relates it to the overall summary.\r\n";
        Instructions += "- RightTopic: A short, 2 to 3 word descriptive topic for the right set of messages that distinguishes it from the left set of messages and relates it to the overall summary.";

        var answerScore = answerVector.PositionOnAxis(facet.Vector);
        var leftMessages = facet.Signposts.Where(x => x.Score < answerScore);
        var rightMessages = facet.Signposts.Where(x => x.Score >= answerScore);

        Text += "\r\n\r\nLeft Messages: ";
        foreach (var message in leftMessages)
            Text += "\r\n" + SafeText(message.Item);

        Text += "\r\n\r\nRight Messages: ";
        foreach (var message in rightMessages)
            Text += "\r\n" + SafeText(message.Item);
    }

    private void AddMessages(IEnumerable<Post> messages, int tokenLimit)
    {
        foreach (var message in messages)
        {
            if (Text.Length + message.Text!.Length > tokenLimit * 4 * 0.8)
            {
                Text += "\r\n- [Truncated additional messages due to token limit]";
                break;
            }
            Text += "\r\n- " + SafeText(message);
        }
    }

    private void AddMessagesWithWeight(IEnumerable<BlossomScoredVector<Post>> messages, int tokenLimit)
    {
        foreach (var message in messages.OrderByDescending(x => Math.Abs(x.Score)))
        {
            if (Text.Length + message.Item.Text!.Length > tokenLimit * 4 * 0.8)
            {
                Text += "\r\n- [Truncated additional messages due to token limit]";
                break;
            }
            Text += $"\r\n- (Weight: {Math.Abs(message.Score):N2}) " + SafeText(message.Item);
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
            Text += "\r\n- " + SafeText(otherSpace.Summary?.Description);

        Text += "\r\n\r\nThis Room: ";
        Text += "\r\n- " + SafeText(space.Summary?.Description);
    }
}
