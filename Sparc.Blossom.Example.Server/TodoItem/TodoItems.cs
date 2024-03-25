using Sparc.Blossom.Data;

namespace Sparc.Blossom.Example.Single.TodoItem;

public partial class TodoItems(ICommandRunner<TodoItem> commandRunner, IQueryRunner<TodoItem> queryRunner)
    : Aggregate<TodoItem>(commandRunner, queryRunner)
{
    public async Task<IEnumerable<TodoItem>> Open() => await Where(x => x.IsDone == false);
}
