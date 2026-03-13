using Sparc.Blossom.Spaces;

namespace Sparc.Blossom.Content;

public record SpaceDiscoveryResponse(string InitialAnswer, List<string> Facts, List<string> Questions);
internal class SpaceDiscoveryQuestion : BlossomQuestion<SpaceDiscoveryResponse>
{

    public SpaceDiscoveryQuestion(BlossomSpace space, Post question, int count) : base($"Given the state of the argument, provide an initial answer or hypothesis, up to {count} initial facts, and up to {count} Socratic questions to begin exploring the following statement.")
    {
        Instructions = "You are an assistant that transforms a statement into a Bayesian prior answer, an initial exploratory set of facts, and an initial exploratory set of Socratic questions related to the statement.\r\n" +
            $"Analyze the provided statement and extract an initial answer and {count} diverse statements that encourage deep thinking and exploration of the topic from many different angles.";

        if (space.Summary != null)
            Text += "\r\n\r\nCurrent State: " + space.Summary.Description + "\r\n\r\n";

        Text += "\r\n\r\nStatement: " + question.Text;
    }
}

public record BasicResponse(string Text);
internal class AnswerHintQuestion : BlossomQuestion<BasicResponse>
{

    public AnswerHintQuestion(BlossomSpace destination, Post lastPost, List<BlossomScoredVector<Fact>> clues) : base("Given the following question, last post from the user, and clues, suggest the next step for the user to take to uncover the answer:")
    {
        Instructions = "You are a dungeon master that is attempting to guide the user through a dimensional space to uncover a hidden truth.\r\n" +
            "You will receive clues and a weighted score from 0 to 1 representing the strength of the clue and its alignment to the hidden truth. " +
            "Use this information to suggest and hint at the next step for the user to take. " +
            "Do not attempt to answer the question, just to guide the user to the answer.";

        Text += "\r\n\r\nQuestion: " + destination.Summary?.Topic + "\r\n\r\n";
        Text += "Last Post: " + lastPost.Text + "\r\n\r\n";
        foreach (var clue in clues.OrderByDescending(x => x.Score))
            Text += $"Clue: {clue.Item.Text}\r\nScore: {clue.Score}\r\n\r\n";
    }
}
