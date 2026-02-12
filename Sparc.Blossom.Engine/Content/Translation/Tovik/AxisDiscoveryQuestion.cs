using Sparc.Blossom.Spaces;

namespace Sparc.Blossom.Content;

public record AxisDiscoveryResponse(List<string> Statements);
internal class AxisDiscoveryQuestion : BlossomQuestion<AxisDiscoveryResponse>
{

    public AxisDiscoveryQuestion(TextContent question) : base("Provide up to 50 initial statements to begin exploring the following question.")
    {
        Instructions = "You are an assistant that transforms a question into an initial exploratory set of Socratic statements.\r\n" +
            "Analyze the provided question and extract 50 diverse statements that encourage deep thinking and exploration of the topic from many different angles.";

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

    public AnswerHintQuestion(BlossomSpace destination, BlossomPost lastPost, List<AnswerHintInput> clues) : base("Given the following question, last post from the user, and clues, suggest the next step for the user to take to uncover the answer:")
    {
        Instructions = "You are a dungeon master that is attempting to guide the user through a dimensional space to uncover a hidden truth.\r\n" +
            "You will receive clues and a weighted score from 0 to 1 representing the strength of the clue and its alignment to the hidden truth. " +
            "Use this information to suggest and hint at the next step for the user to take. " +
            "Do not attempt to answer the question, just to guide the user to the answer.";

        Text += "\r\n\r\nQuestion: " + destination.Summary?.Topic + "\r\n\r\n";
        Text += "Last Post: " + lastPost.Text + "\r\n\r\n";
        foreach (var clue in clues.OrderByDescending(x => x.Score))
            Text += $"Clue: {clue.Text}\r\nScore: {clue.Score}\r\n\r\n";
    }
}
