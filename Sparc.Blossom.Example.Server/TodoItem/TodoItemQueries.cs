using Ardalis.Specification;

namespace Sparc.Blossom.Example.Single.TodoItem;

public class Open : Specification<TodoItem>
{
    public Open() => Query.Where(x => x.IsDone == false);
}