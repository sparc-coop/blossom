namespace TodoItems;

public class TodoItems(BlossomAggregateOptions<TodoItem> options) : BlossomAggregate<TodoItem>(options)
{
    public BlossomQuery<TodoItem> Open() => Query().Where(x => x.IsDone == false);
    public BlossomQuery<TodoItem> Closed() => Query().Where(x => x.IsDone == true);
}
