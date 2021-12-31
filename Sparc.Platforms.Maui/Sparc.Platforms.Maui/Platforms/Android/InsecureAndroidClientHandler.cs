using Android.Net;
using Javax.Net.Ssl;
using Xamarin.Android.Net;

namespace Sparc.Platforms.Maui
{
    public class IgnoreSSLClientHandler : AndroidClientHandler
    {
        protected override SSLSocketFactory ConfigureCustomSSLSocketFactory(HttpsURLConnection connection)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return SSLCertificateSocketFactory.GetInsecure(1000, null);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        protected override IHostnameVerifier GetSSLHostnameVerifier(HttpsURLConnection connection)
        {
            return new IgnoreSSLHostnameVerifier();
        }
    }

    internal class IgnoreSSLHostnameVerifier : Java.Lang.Object, IHostnameVerifier
    {
        public bool Verify(string hostname, ISSLSession session)
        {
            return true;
        }
    }
}
