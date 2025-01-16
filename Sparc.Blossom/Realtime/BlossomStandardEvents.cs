namespace Sparc.Blossom;

public class BlossomEntityAdded<T>(T Entity) : BlossomEvent<T>(Entity) where T : BlossomEntity;
public class BlossomEntityUpdated<T>(string commandName, T Entity) : BlossomEvent<T>(commandName, Entity) where T : BlossomEntity;
public class BlossomEntityPatched<T>(T Entity, BlossomPatch changes) : BlossomEvent<T>(Entity, changes) where T : BlossomEntity;
public class BlossomEntityDeleted<T>(T Entity) : BlossomEvent<T>(Entity) where T : BlossomEntity;

public class BlossomEntityUndone<T> : BlossomEvent<T> where T : BlossomEntity
{
    public BlossomEntityUndone(BlossomEvent<T> current, BlossomEvent<T> previous) : base(previous.Entity, current)
    {
        FutureIds = current.FutureIds;
        FutureIds.Insert(0, current.Id);
    }
}

public class BlossomEntityRedone<T> : BlossomEvent<T> where T : BlossomEntity
{
    public BlossomEntityRedone(BlossomEvent<T> current, BlossomEvent<T> replaceWith) : base(replaceWith.Entity, current)
    {
        FutureIds = current.FutureIds.Skip(FutureIds.IndexOf(replaceWith.Id) + 1).ToList();
    }
}