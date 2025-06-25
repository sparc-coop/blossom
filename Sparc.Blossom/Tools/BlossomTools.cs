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

    public static string FriendlyUsername()
    {
        var client = new HttpClient();
        //var webRequest = new HttpRequestMessage(HttpMethod.Get, "https://blossom-cloud.azurewebsites.net/tools/friendlyusername");
        var webRequest = new HttpRequestMessage(HttpMethod.Get, "https://localhost:7185/tools/friendlyusername");
        var response = client.Send(webRequest);
        if (!response.IsSuccessStatusCode)
            return "User";

        using var reader = new StreamReader(response.Content.ReadAsStream());
        return reader.ReadToEnd().Trim();
    }
}
