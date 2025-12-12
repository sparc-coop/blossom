namespace Sparc.Blossom;

public record BlossomEntityUndone<T> : BlossomEntityChanged<T> where T : BlossomEntity
{
    public BlossomEntityUndone(BlossomEntityChanged<T> current, BlossomEntityChanged<T> previous) : base(previous.Entity, current)
    {
        FutureIds = current.FutureIds;
        FutureIds.Insert(0, current.Id);
    }
}

public record BlossomEntityRedone<T> : BlossomEntityChanged<T> where T : BlossomEntity
{
    public BlossomEntityRedone(BlossomEntityChanged<T> current, BlossomEntityChanged<T> replaceWith) : base(replaceWith.Entity, current)
    {
        FutureIds = current.FutureIds.Skip(FutureIds.IndexOf(replaceWith.Id) + 1).ToList();
    }
}