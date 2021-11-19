using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Text.Json.Serialization;

namespace Sparc.Authentication.Blazor
{
    public class CustomUserAccount : RemoteUserAccount
    {
        [JsonPropertyName("groups")]
        public string[] Groups { get; set; } = new string[] { };

        [JsonPropertyName("roles")]
        public string[] Roles { get; set; } = new string[] { };
    }
}
