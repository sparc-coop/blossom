﻿using Sparc.Blossom.Realtime;

namespace Sparc.Blossom.Api;

public interface IBlossomEntityProxy
{
    string SubscriptionId { get; }
    void Update(IEnumerable<BlossomPatch> patches);
}

public interface IBlossomProxy<T>
{
    IRunner<T> Runner { get; set; }
}

public class BlossomProxy<T> : IBlossomProxy<T>
{
    public IRunner<T> Runner { get; set; } = null!;
}

public class BlossomEntityProxy<T, TId> : BlossomProxy<T>, IBlossomEntityProxy
{
    public TId Id { get; set; } = default!;
    public string SubscriptionId => $"{GetType().Name}-{Id}";

    public void Update(IEnumerable<BlossomPatch> patches)
    {
        foreach (var patch in patches)
            patch.ApplyTo(this);
    }
}