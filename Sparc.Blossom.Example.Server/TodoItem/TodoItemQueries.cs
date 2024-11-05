using Sparc.Blossom.Data;
using Ardalis.Specification;

namespace TodoItems;

public class Open : BlossomQuery<TodoItem>
{
    public Open() => Query.Where(x => x.IsDone == false);
}

public class Closed : BlossomQuery<TodoItem>
{
    public Closed() => Query.Where(x => x.IsDone == true);
}