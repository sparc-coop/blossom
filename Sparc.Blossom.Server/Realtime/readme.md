# Realtime

This documentation will walk you through the steps to enable realtime features in your project, Sparc.Kernel comes with a realtime layer built on top of [SignalR](https://learn.microsoft.com/en-us/aspnet/signalr/overview/getting-started/introduction-to-signalr) and MediatR
With a Realtime feature you can broadcast and listen to notifications in multiple diferent components and propagate your changes everywhere at realtime.
We'll exemplify this with a real example from our Ibis project, where different users in a Room will see the changes in case a text message changes.


## Get Started with Realtime
1. After installing `Sparc.kernel` add the following lines to your `Program.cs` at your Features Project

```csharp
  builder.Services.AddSparcRealtime<IbisHub>();
  //...
  app.MapHub<IbisHub>("/hub");
```
and at your `_Imports.cs` add
```csharp
global using Sparc.Realtime;
global using Sparc.Blossom;
```

2. Create you `SparcHub` class as follows: 
```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Ibis.Features._Plugins;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class IbisHub : SparcHub
{
    public IRepository<User> Users { get; }
    public IRepository<Room> Rooms { get; }
    public IListener Listener { get; }

    public IbisHub(IRepository<User> users, IRepository<Room> rooms, IListener listener) : base()
    {
        Users = users;
        Rooms = rooms;
        Listener = listener;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        
        if (Context.UserIdentifier != null)
            await Users.ExecuteAsync(Context.UserIdentifier, u => u.GoOnline(Context.ConnectionId));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.UserIdentifier != null)
            await Users.ExecuteAsync(Context.UserIdentifier, u => u.GoOffline());

        await base.OnDisconnectedAsync(exception);
    }

    public async Task ReceiveAudio(IAsyncEnumerable<byte[]> audio)
    {
        var sessionId = await Listener.BeginListeningAsync();

        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

        await foreach (var chunk in audio)
            await Listener.ListenAsync(sessionId, chunk);
    }
}
```
> check this file source code [here](https://github.com/sparc-coop/ibis/blob/main/Ibis.Features/_Plugins/IbisHub.cs)

3. Create your Events/Notifications records, you can create a file or add a record as this example to your feature:
```csharp
public record MessageTextChanged(Message Message) : Notification(Message.RoomId + "|" + Message.Language);
```
> More examples: [MessageNotifications.cs](https://github.com/sparc-coop/ibis/blob/main/Ibis.Features/Messages/Entities/MessageNotifications.cs), [TranslateExistingMessages.cs](https://github.com/sparc-coop/ibis/blob/main/Ibis.Features/Messages/TranslateExistingMessages.cs)
> and [Notification class from Sparc.Blossom.Core](Sparc.Blossom.Core/Realtime/Notification.cs)

4. Create your `RealtimeFeature`, a feature that reacts to your event, your feature should follow this example
```csharp
  namespace Ibis.Features.Messages;

  public class SpeakMessage : RealtimeFeature<MessageTextChanged>
  {
      public SpeakMessage(ISpeaker synthesizer, IRepository<Message> messages)
      {
          Synthesizer = synthesizer;
          Messages = messages;
      }

      public ISpeaker Synthesizer { get; }
      public IRepository<Message> Messages { get; }

      public override async Task ExecuteAsync(MessageTextChanged notification)
      {
          await notification.Message.SpeakAsync(Synthesizer);
          await Messages.UpdateAsync(notification.Message);
      }
  }
```

5. Broadcast
```csharp
  Broadcast(new MessageTextChanged(this));
```

> Example in `SetTags` method [Message.cs](https://github.com/sparc-coop/ibis/blob/main/Ibis.Features/Messages/Entities/Message.cs)

6. Listen, at your web project you can listen to any changes creating a components that inherits from `RealtimeComponent` and subscribing to your notification record like
```razor
  @inherits RealtimeComponent
```

```csharp
  await On<MessageTextChanged>(subscriptionId, x =>
  {
      AddMessage(x.Message);
  });
```

Realtime component example with multiple subscriptions to `MessageTextChanged`, `MessageAudioChanged`, `MessageDeleted`
```razor
@inherits RealtimeComponent

<div class="message-grid">
    <div class="message-list ibis-ignore" id="room-@Room.RoomId">
        @foreach (var message in Messages)
        {
            @if (Room.RoomType == "Content")
            {
                <span>@message.Tag</span>
            }
            <MessageCard Message="message" OnEdit=SelectMessage OnDelete=DeleteMessage />
        }
    </div>
    <NewMessage Room="Room" SelectedMessage="SelectedMessage" OnDoneEditing=DoneEditing />
</div>

@code {
    [Parameter] public GetRoomResponse Room { get; set; } = null!;
    List<Message> Messages = new();
    Message? SelectedMessage;
    string? Language;
    bool HasNewMessage;

    protected override async Task OnInitializedAsync()
    {
        var response = await Api.GetAllMessagesAsync(new GetAllMessagesRequest { RoomId = Room.RoomId });
        if (Room.RoomId == "Content")
            Messages = response.Messages.OrderBy(x => x.Tag).ToList();
        else
            Messages = response.Messages.OrderBy(x => x.Timestamp).ToList();
            
        HasNewMessage = true;
        Language = response.Language;

        var subscriptionId = Room.RoomId + "|" + Language;
        await On<MessageTextChanged>(subscriptionId, x =>
        {
            AddMessage(x.Message);
        });

        await On<MessageAudioChanged>(subscriptionId, x =>
        {
            AddMessage(x.Message);
        });
        await On<MessageDeleted>(subscriptionId, x => RemoveMessage(x.Message));
    }
    //...
}
```
