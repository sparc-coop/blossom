using Sparc.Blossom.Data;

namespace TodoItems;

public partial class TodoItem : BlossomEntity<string>
{
    public TodoItem(string title, string description) : base(Guid.NewGuid().ToString())
    {
        Title = title;
        Description = description;
    }


    internal string? ListId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public bool IsDone { get; set; }

    public void MarkDone()
    {
        IsDone = true;
    }

    internal void MoveToNewList(string listId)
    {
        ListId = listId;
    }

    public void MarkUndone()
    {
        IsDone = false;
    }

    public void ClearTitle()
    {
        Title = "";
    }
}
