using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
        var childComponentType = this.GetType();

        var properties = childComponentType.GetProperties();

        foreach (var property in properties)
        {
            // Check if the property has the [Parameter] attribute - future check how we could handle this
            if (property.GetCustomAttributes(typeof(ParameterAttribute), false).Any())
            {
                var value = property.GetValue(this); 
                var valueType = value?.GetType(); 

                if (valueType != null && valueType.Namespace != null)
                {
                    if (valueType.Namespace.StartsWith("Sparc.Blossom.Api"))
                    {
                        var typeName = valueType.Name;

                        var idProperty = valueType.GetProperty("Id",
                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                        if (idProperty != null)
                        {
                            var idValue = idProperty.GetValue(value);

                            var result = $"{typeName} - Id: {idValue}";
                            Console.WriteLine(result);

                            await RegisterBlossomSubscriberAsync(property, childComponentType, typeName, idValue);

                        }
                        else
                        {
                            Console.WriteLine($"The type {typeName} does not contain an 'Id' property.");
                        }
                    }
                }
            }
        }
    }

    private async Task RegisterBlossomSubscriberAsync(PropertyInfo property, Type childComponentType, string typeName, object? idValue)
    {
        var apiFieldInfo = childComponentType.GetRuntimeFields().Where(x => x.Name.Contains("<Api>")).FirstOrDefault();

        if (apiFieldInfo != null)
        {
            var apiField = apiFieldInfo.GetValue(this);

            if (apiField != null)
            {
                var relevantProperty = apiField.GetType().GetProperty(typeName + "s");

                if (relevantProperty != null)
                {
                    var relevantInstance = relevantProperty.GetValue(apiField);

                    if (relevantInstance != null)
                    {
                        var getMethod = relevantInstance.GetType().GetMethod("Get");

                        if (getMethod != null)
                        {
                            await BlossomOn<BlossomEntityChanged>($"{typeName}-{idValue}", async (res) =>
                            {
                                Console.WriteLine("Post updated from raw On", res);
                                var fetchedTask = (Task)getMethod.Invoke(relevantInstance, new object[] { idValue });

                                await fetchedTask.ConfigureAwait(false);
                                var resultProperty = fetchedTask.GetType().GetProperty("Result");
                                var fetchedObject = resultProperty?.GetValue(fetchedTask);

                                if (fetchedObject != null)
                                {
                                    property.SetValue(this, fetchedObject);
                                    InvokeAsync(StateHasChanged);
                                }
                                Console.WriteLine($"Fetched object: {fetchedObject}");
                            });
                        }
                        else
                        {
                            Console.WriteLine($"No 'Get' method found on '{typeName}s'.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"'{typeName}s' instance not found.");
                    }
                }
                else
                {
                    Console.WriteLine("'Api' field value is null.");
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

    protected async Task RawOn<String>(string subscriptionId, Action<String> action) 
    {
        if (Hub != null)
        {
            if (Hub.State == HubConnectionState.Connected)
            {
                await Hub!.InvokeAsync("Watch", subscriptionId);

                Hub.On<String>(subscriptionId, (evt) =>
                {
                    action.Invoke(evt);
                    InvokeAsync(StateHasChanged);
                });
            }
            else
            {
                Hub.On("_UserConnected", async () =>
                {
                    await Hub!.InvokeAsync("Watch", subscriptionId);

                    Hub.On<String>(subscriptionId, (evt) =>
                    {
                        action.Invoke(evt);
                        InvokeAsync(StateHasChanged);
                    });
                });
            }

            //if (!Subscriptions.TryGetValue(subscriptionId, out int value))
            //{
            //    Subscriptions.Add(subscriptionId, 1);
            //    if (Hub.State == HubConnectionState.Connected)
            //    {                    
            //        await Hub!.InvokeAsync("Watch", subscriptionId);
            //    }
            //    else
            //        Hub.On("_UserConnected", async () =>
            //        {
            //            await Hub!.InvokeAsync("Watch", subscriptionId);
            //        });
            //}
            //else
            //{
            //    Subscriptions[subscriptionId] = ++value;
            //}



        }
    }

    protected async Task BlossomOn<T>(string subscriptionId, Action<String> action) where T : BlossomEvent
    {
        if (Hub != null)
        {
            if (Hub.State == HubConnectionState.Connected)
            {
                await Hub!.InvokeAsync("Watch", subscriptionId);

                var test = typeof(T).Name;

                Hub.On<String>(typeof(T).Name, (evt) =>
                {
                    //todo check if equals is necessary?
                    var equals = evt == subscriptionId;
                    if (equals)
                    {
                        action.Invoke(evt);
                        InvokeAsync(StateHasChanged);
                    }
                });
            }
            else
            {
                Hub.On("_UserConnected", async () =>
                {
                    await Hub!.InvokeAsync("Watch", subscriptionId);

                    Hub.On<String>(typeof(T).Name, (evt) =>
                    {
                        var equals = evt == subscriptionId;
                        if (equals)
                        {
                            action.Invoke(evt);
                            InvokeAsync(StateHasChanged);
                        }
                    });
                });
            }

            //if (!Subscriptions.TryGetValue(subscriptionId, out int value))
            //{
            //    Subscriptions.Add(subscriptionId, 1);
            //    if (Hub.State == HubConnectionState.Connected)
            //    {                    
            //        await Hub!.InvokeAsync("Watch", subscriptionId);
            //    }
            //    else
            //        Hub.On("_UserConnected", async () =>
            //        {
            //            await Hub!.InvokeAsync("Watch", subscriptionId);
            //        });
            //}
            //else
            //{
            //    Subscriptions[subscriptionId] = ++value;
            //}



        }
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