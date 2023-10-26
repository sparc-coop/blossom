using Sparc.Blossom.Data;

namespace Sparc.Blossom.Example.Single.TodoItem;

public class TodoItems : BlossomRepository<TodoItem>
{
    public IEnumerable<TodoItem> Open(IRepository<TodoItem> items) => items.Query.Where(x => x.IsDone == false);
}
