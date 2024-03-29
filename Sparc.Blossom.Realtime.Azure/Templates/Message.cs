﻿using System.Reflection;

namespace Sparc.Blossom.Realtime;

public class Message
{
    public Message(string title, string body, MessagePriorities priority = MessagePriorities.Normal)
    {
        Title = title;
        Body = body;
        Priority = priority == MessagePriorities.Normal ? "normal" : "high";
    }

    public string Title { get; set; }
    public string Body { get; set; }
    public string? Image { get; set; }
    public string Priority { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public string? Sound { get; set; }
    public string? ClickAction { get; set; }
    public string? Channel { get; set; }
    public string? ClientPriority { get; set; }
    public string? Visibility { get; set; }

    internal IDictionary<string, string?> ToDictionary()
    {
        var result = GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(
                prop => prop.Name.ToLower(), 
                prop => (string?)prop.GetValue(this, null));

        // Don't serialize null properties
        foreach (var key in result.Keys.Where(key => result[key] == null).ToList())
            result.Remove(key);

        return result;
    }
}

public enum MessagePriorities
{
    Normal,
    High
}