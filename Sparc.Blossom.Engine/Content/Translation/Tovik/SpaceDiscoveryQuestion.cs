using Sparc.Blossom.Spaces;

namespace Sparc.Blossom.Content;

public record SpaceDiscoveryResponse(string InitialAnswer, List<string> Facts, List<string> Questions);
internal class SpaceDiscoveryQuestion : BlossomQuestion<SpaceDiscoveryResponse>
{

    public SpaceDiscoveryQuestion(Post question) : base("Provide an initial answer or hypothesis, up to 20 initial facts, and up to 10 Socratic questions to begin exploring the following question.")
    {
        Instructions = "You are an assistant that transforms a question into a Bayesian prior answer, an initial exploratory set of facts, and an initial exploratory set of Socratic questions related to the question.\r\n" +
            "Analyze the provided question and extract an initial answer and 50 diverse statements that encourage deep thinking and exploration of the topic from many different angles.";

        Text += "\r\n\r\nQuestion: " + question.Text;
    }
}

internal class BestGuessAnswer : BlossomQuestion<BasicResponse>
{
    public BestGuessAnswer(TextContent question) : base("Given the following question, formulate an initial answer:")
    {
        Instructions = "You are an assistant for an epistemic knowledge tool.\r\n" +
            "You will receive a question, and your task is to formulate an initial answer based on the information provided." +
            "This answer will be used as a Bayesian prior to set an initial target in an epistemic space.";

        Text += "\r\n\r\nQuestion: " + question.Text;
    }
}

public record AnswerHintInput(string Text, double Score);
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
