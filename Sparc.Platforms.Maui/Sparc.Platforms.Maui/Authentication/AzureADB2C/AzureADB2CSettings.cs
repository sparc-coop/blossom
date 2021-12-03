using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sparc.Platforms.Maui
{
    public class AzureADB2CSettings
    {
        public AzureADB2CSettings(string hostname,
            string clientId,
            string scope,
            string signInPolicy = "B2C_1_SignIn_SignUp",
            string editProfilePolicy = "b2c_1_edit_profile",
            string resetPasswordPolicy = "b2c_1_reset",
            Func<object> parentWindowLocator = null)
        {
            Tenant = $"{hostname}.onmicrosoft.com";
            Hostname = $"{hostname}.b2clogin.com";
            ClientID = clientId;
            PolicySignUpSignIn = signInPolicy;
            PolicyEditProfile = editProfilePolicy;
            PolicyResetPassword = resetPasswordPolicy;
            ParentWindowLocator = parentWindowLocator;
            Scopes = new[] { "openid", "offline_access", $"https://{Tenant}/{ClientID}/{scope}" };
        }

        // Azure AD B2C Coordinates
        public string Tenant { get; }
        public string Hostname { get; }
        public string ClientID { get; }
        public string PolicySignUpSignIn { get; }
        public string PolicyEditProfile { get; }
        public string PolicyResetPassword { get; }
        public Func<object> ParentWindowLocator { get; }
        public string[] Scopes { get; }

        public string AuthorityBase => $"https://{Hostname}/tfp/{Tenant}/";
        public string AuthoritySignInSignUp => $"{AuthorityBase}{PolicySignUpSignIn}";
        public string AuthorityEditProfile => $"{AuthorityBase}{PolicyEditProfile}";
        public string AuthorityPasswordReset => $"{AuthorityBase}{PolicyResetPassword}";
        public string IOSKeyChainGroup => "com.microsoft.adalcache";
        public string DataScheme => $"msal{ClientID}";

        public object ParentWindow => ParentWindowLocator == null ? null : ParentWindowLocator();
    }
}
