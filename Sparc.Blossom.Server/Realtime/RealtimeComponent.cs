using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using Sparc.Blossom.Data;
using System.Reflection;

namespace Sparc.Blossom.Realtime;

public class BlossomRealtime : ComponentBase
{
    [CascadingParameter] public HubConnection? Hub { get; set; }
    readonly List<IDisposable> Events = [];
    protected readonly static Dictionary<string, int> Subscriptions = [];
    protected List<string> LocalSubscriptions = [];


    protected override async Task OnInitializedAsync()
    {
        await SubscribeToBlossomEntityChanges();
    }

    private async Task SubscribeToBlossomEntityChanges()
    {
        var childComponentType = this.GetType();

        var properties = childComponentType.GetProperties();

        foreach (var property in properties)
        {
            var value = property.GetValue(this);
            var valueType = value?.GetType();

            if (IsNamespaceBlossomApi(valueType))
            {
                var typeName = valueType.Name;

                var idProperty = valueType.GetProperty("Id",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                if (idProperty != null)
                {
                    var idValue = idProperty.GetValue(value);

                    await BlossomOn<BlossomEntityChanged>($"{typeName}-{idValue}", async (res) =>
                    {
                        if (res.Entity != null)
                        {
                            UpdateModifiedProperties(property, this, res.Entity);
                            InvokeAsync(StateHasChanged);
                        }
                    });
                }
                else
                {
                    Console.WriteLine($"The type {typeName} does not contain an 'Id' property.");
                }
            }


        }
    }

    private bool IsNamespaceBlossomApi(Type? valueType)
    {
        if (valueType != null && valueType.Namespace != null && valueType.Namespace.StartsWith("Sparc.Blossom.Api"))
        {
            return true;
        }
        
        return false;
    }

    private void UpdateModifiedProperties(PropertyInfo targetProperty, object targetObject, object sourceObject)
    {
        var targetType = targetProperty.PropertyType;
        var sourceProperties = sourceObject.GetType().GetProperties();

        foreach (var sourceProp in sourceProperties)
        {
            if (sourceProp.Name == "Id")
            {
                continue; 
            }

            var targetProp = targetType.GetProperty(sourceProp.Name);
            if (targetProp != null && targetProp.CanWrite)
            {
                var sourceValue = sourceProp.GetValue(sourceObject);
                var targetValue = targetProp.GetValue(targetProperty.GetValue(targetObject));

                if (!Equals(targetValue, sourceValue))
                {
                    targetProp.SetValue(targetProperty.GetValue(targetObject), sourceValue);
                }
            }
        }
    }

    protected void On<T>(Action<T> action)
    {
        if (Hub != null)
            Events.Add(Hub.On<T>(typeof(T).Name, evt =>
            {
                action(evt);
                StateHasChanged();
            }));
    }

    protected async Task BlossomOn<T>(string subscriptionId, Action<BlossomEvent> action) where T : BlossomEvent
    {
        if (Hub != null)
        {
            if (Hub.State == HubConnectionState.Connected)
            {
                await AddSignalRHandler<T>(subscriptionId, action);
            }
            else
            {
                Hub.On("_UserConnected", async () =>
                {
                    await AddSignalRHandler<T>(subscriptionId, action);
                });
            }
        }
    }

    private async Task AddSignalRHandler<T>(string subscriptionId, Action<BlossomEvent> action) where T : BlossomEvent
    {
        await Hub!.InvokeAsync("Watch", subscriptionId);

        Hub.On(typeof(T).Name, (Action<string>)((json) =>
        {
            BlossomEvent? evt = DeserializeNotificationObject(json);

            var equals = evt.SubscriptionId == subscriptionId;
            if (equals)
            {
                action.Invoke(evt);
                InvokeAsync(StateHasChanged);
            }
        }));
    }

    private static BlossomEvent? DeserializeNotificationObject(string json)
    {
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        var evt = JsonConvert.DeserializeObject<BlossomEvent>(json, settings);
        return evt;
    }

    protected async Task On<T>(string subscriptionId, Action<T> action) where T : BlossomEvent
    {
        if (Hub != null)
        {
            if (!Subscriptions.TryGetValue(subscriptionId, out int value))
            {
                Subscriptions.Add(subscriptionId, 1);
                if (Hub.State == HubConnectionState.Connected)
                {
                    await Hub!.InvokeAsync("Watch", subscriptionId);
                }
                else
                    Hub.On("_UserConnected", async () =>
                    {
                        await Hub!.InvokeAsync("Watch", subscriptionId);
                    });
            }
            else
            {
                Subscriptions[subscriptionId] = ++value;
            }

            LocalSubscriptions.Add(subscriptionId);

            Events.Add(Hub.On<T>(typeof(T).Name, evt =>
            {

                var equals = evt.SubscriptionId == subscriptionId;
                if (equals)
                {
                    action(evt);
                    StateHasChanged();
                }
            }));
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var subscription in LocalSubscriptions.ToList())
        {
            if (Subscriptions.TryGetValue(subscription, out int value))
            {
                Subscriptions[subscription] = --value;
                if (value <= 0)
                {
                    Subscriptions.Remove(subscription);
                    if (Hub?.State == HubConnectionState.Connected)
                        await Hub!.InvokeAsync("StopWatching", subscription);
                }
            }

            foreach (var evt in Events)
                evt.Dispose();

            Events.Clear();
            LocalSubscriptions.Clear();
        }
    }
}