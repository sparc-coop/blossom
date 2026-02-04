namespace Sparc.Blossom.Content;

public record AxisDiscoveryResponse(List<string> SocraticStatements);
internal class AxisDiscoveryQuestion : BlossomQuestion<AxisDiscoveryResponse>
{

    public AxisDiscoveryQuestion(TextContent question) : base("Provide 5 initial progressive Socratic statements to begin exploring the following question.")
    {
        Instructions = "You are an assistant that transforms a question into an initial exploratory set of Socratic questions and/or statements.\r\n" +
            "Analyze the provided question and extract 5 progressive Socratic statements that encourage deep thinking and exploration of the topic.";

        Text += "\r\n\r\nQuestion: " + question.Text;
    }
}
