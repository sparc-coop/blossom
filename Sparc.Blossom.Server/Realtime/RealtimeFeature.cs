﻿using MediatR;

namespace Sparc.Blossom.Realtime;

public abstract class RealtimeFeature<T> : INotificationHandler<T> where T : Notification
{
    public abstract Task ExecuteAsync(T item);

    public async Task Handle(T request, CancellationToken cancellationToken)
    {
        await ExecuteAsync(request);
    }
}