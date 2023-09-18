using Sparc.Blossom.Data;

namespace Sparc.Blossom.Example.Single.TodoItem;

public class TodoItem(string title, string description) : Entity<string>(Guid.NewGuid().ToString())
{
    public string? ListId { get; set; }
    public string Title { get; set; } = title;
    public string Description { get; set; } = description;
    public bool IsDone { get; set; }

    public void MarkDone()
    {
        IsDone = true;
    }

    public void MoveToNewList(string listId)
    {
        ListId = listId;
    }
}
