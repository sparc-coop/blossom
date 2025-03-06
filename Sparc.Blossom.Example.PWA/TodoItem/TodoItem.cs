﻿namespace TodoItems;

public partial class TodoItem(string title, string description) : BlossomEntity<string>(Guid.NewGuid().ToString())
{
    internal string? ListId { get; set; }
    public string Title { get; set; } = title;
    public string Description { get; set; } = description;
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
