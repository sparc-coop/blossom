namespace TodoItems;

public class TodoItems(BlossomAggregateOptions<TodoItem> options, IRandomStringGenerator randomStrings) : BlossomAggregate<TodoItem>(options)
{
    public BlossomQuery<TodoItem> Open() => Query().Where(x => x.IsDone == false);
    public BlossomQuery<TodoItem> Closed() => Query().Where(x => x.IsDone == true);

    public Task<IEnumerable<string>> RandomStrings(int count)
    {
        return Task.FromResult(Enumerable.Range(0, count).Select(x => randomStrings.GenerateRandomString()));
    }
}
