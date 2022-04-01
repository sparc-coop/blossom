using Blazored.LocalStorage;
using Sparc.Core;
using System;

namespace Sparc.Platforms.Web
{
    public class WebDeviceTokenProvider : ITokenProvider
    {
        public WebDeviceTokenProvider(ISyncLocalStorageService localStorage)
        {
            LocalStorage = localStorage;
        }

        private string _token;
        public string Token
        {
            get
            {
                if (_token != null)
                    return _token;

                try
                {

                    if (LocalStorage.ContainKey("sparc-device-id"))
                        _token = LocalStorage.GetItemAsString("sparc-device-id");
                    else
                    {
                        _token = Guid.NewGuid().ToString();
                        LocalStorage.SetItemAsString("sparc-device-id", _token);
                    }
                }
                catch { }

                return _token;
            }
            set {  _token = value; }
        }
        public ISyncLocalStorageService LocalStorage { get; }
    }
}
