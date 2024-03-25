using Sparc.Blossom.Data;

namespace Sparc.Blossom.Example.Single.TodoItem;

public partial class TodoItems
{
    public IEnumerable<TodoItem> Open(IRepository<TodoItem> items) => items.Query.Where(x => x.IsDone == false);
}
