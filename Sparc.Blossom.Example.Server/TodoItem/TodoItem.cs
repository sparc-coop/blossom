using Sparc.Blossom.Data;

namespace Sparc.Blossom.Example.Single.TodoItem;

public class TodoItem(string title, string description) : Entity<string>
{
    private string? _listId;
    public string Title { get; set; } = title;
    public string Description { get; set; } = description;
    public bool IsDone { get; set; }

    public void MarkDone()
    {
        IsDone = true;
    }

    internal void MoveToNewList(string listId)
    {
        _listId = listId;
    }
}
