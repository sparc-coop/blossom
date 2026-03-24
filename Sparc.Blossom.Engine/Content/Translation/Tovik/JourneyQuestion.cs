using Sparc.Blossom.Spaces;

namespace Sparc.Blossom.Content;

public record BlossomJourney(string Hint, List<string> Facts, List<string> Questions);

internal class JourneyQuestion : BlossomQuestion<BlossomJourney>
{
    public JourneyQuestion(BlossomSpace userSpace, List<QuestPath> paths)
        : base(
    "Given the following set of messages which represent a sequential journey, left to right, and the user's current location, guide the user toward the stated exit location, via the conflict or tension located within." +
    " This will be used for a game quest description, so phrase accordingly.")
    {
        Instructions = "You are an assistant that takes a sequential journey of relevant facts as signposts along the way, a user's location, and an exit location, and guides the user accordingly.\r\n" +
                    "Analyze the provided messages and extract the main themes from the messages to guide the user from their current location toward the exit location of the quest, whether that's left or right, via the conflict or tension located within.\r\n\r\n" +
                    "The output should include:\r\n" +
                    "- Hint: A 10-20 word hint as to how to navigate this journey from the user's current location to the exit location.\r\n" +
                    "- Facts: A list of key verified facts with a preponderance of evidence in the world which are relevant to this journey.\r\n" +
                    "- Questions: A list of 3 to 5 Socratic questions that use the facts along the way to encourage the user to move toward the exit.\r\n";

        Text += "\r\n\r\nUser's Current Location: " + SafeText(QuestPath.Closest(paths, userSpace)?.Signpost);
        Text += "\r\n\r\nExit Location: " + SafeText(paths.LastOrDefault()?.Signpost);
        Text += "\r\n\r\nSequential Journey (left to right): ";
        foreach (var message in paths.OrderBy(x => x.Index))
            Text += "\r\n- " + SafeText(message.Vector.Text);
    }
}
