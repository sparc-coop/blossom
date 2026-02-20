using Sparc.Blossom.Spaces;

namespace Sparc.Blossom.Content;

internal class JourneyQuestion : BlossomQuestion<BlossomSummary>
{
    public JourneyQuestion(IEnumerable<BlossomScoredVector<Post>> messages, BlossomScoredVector<Post> userLocation, BlossomScoredVector<Post> exitLocation)
        : base(
    "Given the following set of messages which represent a sequential journey, left to right, and the user's current location, guide the user toward the exit, via the conflict or tension located within." +
    " This will be used for a game quest description, so phrase accordingly.")
    {
        Instructions = "You are an assistant that summarizes a series of messages into a new semantic facet that will be used for a game quest.\r\n" +
                    "Analyze the provided messages and extract the main themes from the messages to guide the user from their current location toward the last message of the quest, via the conflict or tension located within.\r\n\r\n" +
                    "The summary should include:\r\n" +
                    "- Name: A short, 2 to 3 word descriptive quest title encompassing the journey to take to reach the right side. This name should be extremely specific to the primary subject matter of the quest.\r\n" +
                    "- Topic: The 10-20 word primary subject matter represented by the journey from the user's current location to the right side. This should be phrased as a game quest description.\r\n" +
                    "- Description: A 10-20 word set of hints as to how to navigate this journey.";

        Instructions += "- LeftTopic: A short, 2 to 3 word descriptive topic for the left set of messages that distinguishes it from the right set of messages and relates it to the overall summary.\r\n";
        Instructions += "- RightTopic: A short, 2 to 3 word descriptive topic for the right set of messages that distinguishes it from the left set of messages and relates it to the overall summary.";

        Text += "\r\n\r\nUser's Current Location: " + SafeText(userLocation.Item);
        Text += "\r\n\r\nExit Location: " + SafeText(exitLocation.Item);
        Text += "\r\n\r\nSequential Journey (left to right): ";
        foreach (var message in messages.OrderBy(x => x.Score))
            Text += "\r\n- " + SafeText(message.Item);
    }
}
