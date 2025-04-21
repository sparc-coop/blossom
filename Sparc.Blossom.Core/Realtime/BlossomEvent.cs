﻿using Sparc.Blossom.Authentication;
using System.Security.Claims;

namespace Sparc.Blossom;

public record BlossomEvent : MediatR.INotification
{
    public BlossomEvent()
    {
        // for JSON deserialization
        Name = "BlossomEvent";
    }

    private BlossomEvent(string name)
    {
        Name = name;
    }

    public BlossomEvent(BlossomEntity entity) : this(entity.GetType().Name)
    {
        EntityId = entity.GenericId.ToString();
        EntityType = entity.GetType().Name;
    }

    public BlossomEvent(string name, BlossomEntity entity) : this(entity)
    {
        Name = name;
    }

    public BlossomEvent(IBlossomEntityProxy proxy) : this(proxy.GetType().Name)
    {
        EntityType = proxy.GetType().Name;
    }

    public string Name { get; set; }
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public long Id { get; set; } = DateTime.UtcNow.Ticks;
    public string? UserId { get; set; }
    public BlossomPatch? Changes { get; set; } = null;
    public long? PreviousId { get; set; }
    public List<long> FutureIds { get; set; } = [];
    public string? SubscriptionId => string.IsNullOrWhiteSpace(EntityId) ? null : $"{EntityType}-{EntityId}";

    public void SetUser(ClaimsPrincipal? user)
    {
        UserId = user?.Id();
    }

    public void ApplyTo(IBlossomEntityProxy entity)
    {
        Changes?.ApplyTo(entity);
    }
}

public record BlossomEvent<T> : BlossomEvent where T : BlossomEntity
{
    public T Entity { get; private set; }

    public BlossomEvent(T entity) : base(entity)
    {
        Entity = entity;
    }

    public BlossomEvent(BlossomEvent<T> previous) : base(previous)
    {
        Entity = previous.Entity;
    }

    public BlossomEvent(string name, T entity) : this(entity)
    {
        Name = name;
    }

    public BlossomEvent(T entity, BlossomPatch changes) : this(entity)
    {
        Changes = changes;
    }

    public BlossomEvent(T entity, BlossomEvent<T> previous) : this(entity)
    {
        PreviousId = previous.Id;
        Changes = new(previous.Entity, entity);
    }
}
