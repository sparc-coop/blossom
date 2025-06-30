namespace Sparc.Blossom;

public class BlossomTools
{
    public static string FriendlyId()
    {
        var client = new HttpClient();
        var webRequest = new HttpRequestMessage(HttpMethod.Get, "https://engine.sparc.coop/tools/friendlyid");
        var response = client.Send(webRequest);
        if (!response.IsSuccessStatusCode)
            return Guid.NewGuid().ToString();

        using var reader = new StreamReader(response.Content.ReadAsStream());
        return reader.ReadToEnd();
    }
}
