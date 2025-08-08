﻿namespace Sparc.Blossom.Realtime.Matrix;

public class MatrixRoom(string roomId, string? roomType)
{
    public string RoomId { get; set; } = roomId;
    public string? RoomType { get; set; } = roomType;
    public int NumJoinedMembers { get; set; }
    public bool GuestCanJoin { get; set; }
    public bool WorldReadable { get; set; }
    public string? Name { get; set; }
    public string? Topic { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CanonicalAlias { get; set; }
    public string? JoinRule { get; set; }

    public string LocalId => RoomId.Split(':').First();

    public static MatrixRoom From(IEnumerable<MatrixEvent> events)
    {
        var orderedEvents = events.OrderBy(x => x.Depth);

        var rootEvent = events.OfType<MatrixEvent<CreateRoom>>().First();

        var room = new MatrixRoom(rootEvent.RoomId, rootEvent.Content.Type);
        foreach (var ev in orderedEvents)
            ev.ApplyTo(room);

        return room;
    }
}