using Sparc.Blossom.Data;

namespace Sparc.Blossom.Example.Server.TodoItem;

public class TodoItem : Entity<string>
{
    public TodoItem(string title, string description)
    {
        Title = title;
        Description = description;
    }

    internal string UserId { get; set; }
    private string? _listId;
    public string Title { get; set; }
    public string Description { get; set; }
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
