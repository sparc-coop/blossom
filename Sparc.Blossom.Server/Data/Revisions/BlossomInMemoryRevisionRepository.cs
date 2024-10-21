
using System.Security.Claims;
using Sparc.Blossom.Authentication;

namespace Sparc.Blossom.Data;

public class BlossomInMemoryRevisionRepository<T>(IRepository<T> repository) : IRevisionRepository<T> where T : BlossomEntity
{
    // This dictionary's keys are UserId, EntityId
    readonly Dictionary<string, Dictionary<string, List<BlossomRevision<T>>>> _items = [];
    const int MaxRevisions = 10;

    IRepository<T> Repository { get; } = repository;

    Dictionary<string, List<BlossomRevision<T>>> UserItems()
    {
        var userId = ClaimsPrincipal.Current!.Id();
        if (!_items.ContainsKey(userId))
            _items.Add(userId, []);
        return _items[userId];
    }

    List<BlossomRevision<T>> Entity(string id)
    {
        var userItems = UserItems();
        
        if (userItems.ContainsKey(id))
            userItems.Add(id, []);

        return userItems[id];
    }

    public Task<IEnumerable<BlossomRevision<T>>> GetAllAsync(string id, int count)
    {
        var entityRevisions = Entity(id);
        return Task.FromResult(entityRevisions.Take(count));
    }

    public Task<BlossomRevision<T>?> GetAsync(string id, long revision)
    {
        var entityRevisions = Entity(id);
        return Task.FromResult(entityRevisions.FirstOrDefault(x => x.Revision == revision));
    }

    public Task<BlossomRevision<T>?> GetAsync(string id, DateTime? asOfDate = null)
    {
        var entityRevisions = Entity(id);
        var ticks = (asOfDate?.ToUniversalTime() ?? DateTime.UtcNow).Ticks;
        return Task.FromResult(entityRevisions.FirstOrDefault(x => x.Revision <= ticks));
    }

    public async Task<BlossomRevision<T>> RedoAsync(string id)
    {
        var current = await GetAsync(id)
            ?? throw new Exception("No current revision to redo");

        if (current.Future.Count == 0)
            throw new Exception("No future revision to redo");

        return await ReplaceAsync(id, current.Future[0]);
    }

    public async Task<BlossomRevision<T>> ReplaceAsync(string id, long revision)
    {
        var current = await GetAsync(id)
            ?? throw new Exception("No current revision found");

        var replaceWith = await GetAsync(id, revision)
            ?? throw new Exception("Revision to replace with not found");

        var newRevision = current.UpdateTo(replaceWith);
        Add(id, newRevision);
        return newRevision;
    }

    public async Task<BlossomRevision<T>> UndoAsync(string id)
    {
        var current = await GetAsync(id)
            ?? throw new Exception("No current revision to undo");

        if (!current.Previous.HasValue)
            throw new Exception("No previous revision to undo");

        return await ReplaceAsync(id, current.Previous.Value);
    }

    public async Task AddAsync(string id)
    {
        var current = await Repository.FindAsync(id)
            ?? throw new Exception("No current entity to add revision for");
        
        var previous = await GetAsync(id);
        var newRevision = BlossomRevision<T>.Create(current, previous);
        Add(id, newRevision);
    }

    private void Add(string id, BlossomRevision<T> newRevision)
    {
        var entities = Entity(id);
        if (entities.Count == MaxRevisions)
            entities.RemoveAt(MaxRevisions - 1);
        entities.Insert(0, newRevision);
    }
}
