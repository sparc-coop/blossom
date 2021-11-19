using IdentityModel.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sparc.Authentication.Client
{
    public class ApiClient : HttpClient
    {
        public ApiClient(string name, string baseUrl) : base()
        {
            Name = name;
            BaseAddress = new Uri(baseUrl);
        }
        
        private DiscoveryDocumentResponse ServerInfo { get; set; }

        private string Name;

        public async Task DiscoverAsync()
        {
            ServerInfo = await this.GetDiscoveryDocumentAsync(BaseAddress.AbsoluteUri);

            if (ServerInfo.IsError)
                throw new Exception(ServerInfo.Error);
        }

        public async Task RegisterClientAsync(string clientId)
        {
            if (ServerInfo == null)
                await DiscoverAsync();
            
            var response = await this.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = ServerInfo.TokenEndpoint,
                ClientId = clientId,
                Scope = Name.Replace(" ", ".")
            });

            if (response.IsError)
                throw new Exception(response.Error + " " + response.ErrorDescription);

            this.SetBearerToken(response.AccessToken);
        }
    }
}
