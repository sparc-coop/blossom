using Mapster;

namespace Sparc.Blossom;

public class BlossomCollection<T>(IRepository<T> repository)
    where T : BlossomEntity
{
    protected IRepository<T> Repository => repository;

    public virtual async Task<T?> Get(object id) => await Repository.FindAsync(id);

    protected BlossomQuery<T> Query() => new(Repository);

    protected virtual BlossomQuery<T> Query(BlossomQueryOptions options) => Query().WithOptions(options);

    public async Task<BlossomQueryResult<T>> ExecuteQuery(BlossomQueryOptions options)
    {
        var query = Query(options);
        var results = await query.Execute();
        var count = await Repository.CountAsync(query);

        return new BlossomQueryResult<T>(results, count);
    }

    public async Task<IEnumerable<T>> ExecuteQuery(string name, params object?[] parameters)
    {
        // Find the matching method and parameters in this type
        var query = GetType().GetMethod(name)
            ?? throw new Exception($"Method {name} not found.");


        var spec = query.Invoke(this, parameters) as BlossomQuery<T>
            ?? throw new Exception($"Specification {name} not found.");

        return await spec.Execute();
    }

    //public Task<BlossomAggregateMetadata> Metadata()
    //{
    //    var metadata = new BlossomAggregateMetadata(DtoType!);

    //    foreach (var property in metadata.Properties.Where(x => x.CanEdit && x.IsPrimitive))
    //    {
    //        var query = Repository.Query.GroupBy(property.Name).Select("new { Key, Count() as Count }");
    //        property.SetAvailableValues(query.ToDynamicList().ToDictionary(x => (object)x.Key ?? "<null>", x => (int)x.Count));
    //    }

    //    foreach (var relationship in metadata.Properties.Where(x => x.CanEdit && x.IsEnumerable))
    //    {
    //        var query = Repository.Query.SelectMany(relationship.Name).GroupBy("Id").Select("new { Key, Count() as Count, First() as First }").ToDynamicList();
    //        relationship.SetAvailableValues(query.Sum(x => (int)x.Count), query.ToDictionary(x => $"{x.Key}", x => ToDto(x.First)));
    //    }

    //    return Task.FromResult(metadata);
    //}

    //public async Task<T?> Undo(object id, long? revision)
    //{
    //    var strId = id.ToString()!;

    //    return !revision.HasValue
    //        ? await Events.UndoAsync(strId)
    //        : await Events.ReplaceAsync(strId, revision.Value);
    //}

    //public async Task<T?> Redo(object id, long? revision)
    //{
    //    var strId = id.ToString()!;

    //    return !revision.HasValue
    //        ? await Events.RedoAsync(strId)
    //        : await Events.ReplaceAsync(strId, revision.Value);
    //}

    //public Type? DtoType => AppDomain.CurrentDomain.FindType($"Sparc.Blossom.Api.{typeof(T).Name}");

    //public object? ToDto<TItem>(TItem entity)
    //{
    //    var dtoTypeName = $"Sparc.Blossom.Api.{typeof(TItem).Name}";
    //    var dtoType = AppDomain.CurrentDomain.FindType(dtoTypeName);
    //    return dtoType == null ? null : entity!.Adapt(typeof(TItem), dtoType);
    //}
}
