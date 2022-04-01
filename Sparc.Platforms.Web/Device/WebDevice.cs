using Blazored.LocalStorage;
using Sparc.Core;
using System;

namespace Sparc.Platforms.Web
{
    public class WebDevice : Device
    {
        public WebDevice(ISyncLocalStorageService localStorage)
        {
            LocalStorage = localStorage;
        }

        private string _id;
        public override string Id
        {
            get
            {
                if (_id != null)
                    return _id;

                try
                {

                    if (LocalStorage.ContainKey("sparc-device-id"))
                        _id = LocalStorage.GetItemAsString("sparc-device-id");
                    else
                    {
                        _id = Guid.NewGuid().ToString();
                        LocalStorage.SetItemAsString("sparc-device-id", _id);
                    }
                }
                catch { }

                return _id;
            }
            set {  _id = value; }
        }
        public ISyncLocalStorageService LocalStorage { get; }
    }
}
