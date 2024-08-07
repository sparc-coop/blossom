﻿using Microsoft.JSInterop;

namespace Sparc.Blossom.Authentication;

public class WebDevice(IJSRuntime js) : IDevice
{
    private string? _id;
    public string? Id
    {
        get
        {
            _id ??= GetOrSet("blossom-device-id", Guid.NewGuid().ToString());
            return _id;
        }
        set { _id = value; }
    }

    private string? _pushToken;
    public string? PushToken
    {
        get
        {
            _pushToken ??= GetOrSet("blossom-device-pushtoken");            
            return _pushToken;
        }
        set
        {
            _pushToken = value;
            GetOrSet("blossom-device-pushtoken", value, true);
        }
    }

    private string? GetOrSet(string key, string? value = null, bool overwrite = false)
    {
        var result = Js.Invoke<string?>("localStorage.getItem", key);

        if (value != null && (result == null || overwrite))
        {
            Js.InvokeVoid("localStorage.setItem", key, value);
            result = value;
        }

        return result;
    }

    public string Platform => "Web";
    public string? DeviceType { get; set; }
    string? IDevice.Platform { get; set; }
    public string? Idiom { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? Name { get; set; }
    public string? VersionString { get; set; }
    public IJSInProcessRuntime Js { get; } = js as IJSInProcessRuntime
            ?? throw new ArgumentException("WebDevice requires IJSInProcessRuntime", nameof(js));
}
