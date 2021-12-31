using System.Net.Http;

namespace Sparc.Platforms.Maui
{
    public class InsecureHandlerProvider
    {
        public static HttpMessageHandler GetHandler()
        {
            return new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    return true;
                }
            };
        }

        public static bool IsLocal(string baseUrl)
        {
            return baseUrl.StartsWith("https://localhost")
                || baseUrl.StartsWith("https://127.0.0.1")
                || baseUrl.StartsWith("https://10.0.2.2");
        }

    }
}