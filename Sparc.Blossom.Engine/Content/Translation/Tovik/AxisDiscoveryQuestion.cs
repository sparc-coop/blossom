namespace Sparc.Blossom.Content;

public record AxisDiscoveryResponse(string Answer, string ChiefTension);
internal class AxisDiscoveryQuestion : BlossomQuestion<AxisDiscoveryResponse>
{

    public AxisDiscoveryQuestion(TextContent question) : base("Provide the answer and chief tension to the following question.")
    {
        Instructions = "You are an assistant that transforms a question into an initial exploratory answer and the chief tension you expect to encounter in the exploration of the question.\r\n" +
            "Analyze the provided question and extract the following:\r\n\r\n" +
            "- Answer: Your initial exploratory answer to this question. This answer will be used to set the initial goal of an exploratory session, wihch can be adjusted as more discovery is completed.\r\n" +
            "- Chief Tension: The key challenge or conflict that needs to be addressed in the exploration of this question.\r\n";

        Text += "\r\n\r\nQuestion: " + question.Text;
    }
}
