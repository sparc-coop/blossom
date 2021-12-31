using System.Net.Http;

namespace Sparc.Platforms.Maui
{
    public class SparcHttpClientHandler : HttpClientHandler
    {
        public SparcHttpClientHandler()
        {
            // allow connection to local SSL APIs
#if DEBUG
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                if (cert.Issuer.Equals("CN=localhost"))
                    return true;
                return errors == System.Net.Security.SslPolicyErrors.None;
            };
#endif
        }
    }
}
