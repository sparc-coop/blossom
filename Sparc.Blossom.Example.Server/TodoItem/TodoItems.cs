namespace TodoItems;

public class TodoItems(IRepository<TodoItem> options) : BlossomCollection<TodoItem>(options)
{
    public BlossomQuery<TodoItem> Open() => Query().Where(x => x.IsDone == false);
    public BlossomQuery<TodoItem> Closed() => Query().Where(x => x.IsDone == true);
}
