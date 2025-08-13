using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using Sparc.Blossom.Realtime.Matrix;
using System.Collections.Generic;

namespace Sparc.Blossom.Realtime;

public class MatrixEvents(
    IRepository<MatrixEvent> events,
    IHttpContextAccessor http,
    SparcAuthenticator<BlossomUser> auth)
{
    public const string Domain = "sparc.coop";
    public string? MatrixSenderId;

    public async Task<MatrixEvent> PublishAsync<T>(string roomId, T content)
    {
        var sender = await GetMatrixSenderIdAsync();

        var ev = MatrixEvent.Create(roomId, sender, content);
        await events.AddAsync(ev);
        return ev;
    }

    internal async Task<List<MatrixEvent>> GetAllAsync(string roomId, long? since = null)
    {
        var query = events.Query.Where(x => x.RoomId == roomId);

        if (since.HasValue)
            query = query.Where(x => x.OriginServerTs > since.Value);

        return await query
            .OrderBy(x => x.OriginServerTs)
            .ToListAsync();
    }

    internal async Task<List<MatrixEvent<T>>> GetAllAsync<T>(string roomId)
    {
        var type = MatrixEvent.Types<T>();
        var result = await events.Query
            .Where(e => e.RoomId == roomId && e.Type == type)
            .OrderBy(x => x.OriginServerTs)
            .ToListAsync();

        return result.Cast<MatrixEvent<T>>().ToList();
    }

    internal async Task<Rooms> GetRoomUpdatesAsync(long since)
    {
        var userId = await GetMatrixSenderIdAsync();

        var events = await Query<ChangeMembershipState>(since)
            .Where(e => e.StateKey == userId)
            .ToListAsync();

        var membershipChanges = events.Cast<MatrixEvent<ChangeMembershipState>>().ToList();

        var joinedRooms = membershipChanges.Where(e => e.Content.Membership == "join")
            .GroupBy(x => x.RoomId)
            .ToDictionary(x => x.Key, x => new JoinedRoom());

        foreach (var joinedRoom in joinedRooms)
        {
            var allRoomEvents = await GetAllAsync(joinedRoom.Key, since);
            joinedRoom.Value.Timeline.Events.AddRange(allRoomEvents.Where(x => x.Type == MatrixEvent.Types<MatrixMessage>()));
        }

        var invitedRooms = membershipChanges.Where(e => e.Content.Membership == "invite")
            .GroupBy(x => x.RoomId)
            .ToDictionary(x => x.Key, x => new InvitedRoom(new([])));

        foreach (var invitedRoom in invitedRooms)
        {
            var strippedInvites = await GetStrippedStateEventsAsync(invitedRoom.Key, since);
            invitedRoom.Value.InviteState.Events.AddRange(strippedInvites.Select(x => new MatrixStrippedStateEvent(x)));
        }

        var rooms = new Rooms(joinedRooms, invitedRooms, [], []);
        return rooms;
    }

    readonly List<string> StrippedStateTypes =
    [
        MatrixEvent.Types<CreateRoom>(),
        MatrixEvent.Types<RoomName>(),
        MatrixEvent.Types<RoomTopic>(),
        MatrixEvent.Types<JoinRules>(),
        MatrixEvent.Types<CanonicalAlias>()
    ];
    internal async Task<List<MatrixEvent>> GetStrippedStateEventsAsync(string roomId, long? since = null)
    {
        var query = events.Query
            .Where(e => e.RoomId == roomId && StrippedStateTypes.Contains(e.Type));

        if (since.HasValue)
            query = query.Where(e => e.OriginServerTs > since.Value);

        return await query.ToListAsync();
    }

    internal async Task<MatrixRoomSummary> GetRoomAsync(string roomId)
    {
        var allRoomEvents = await GetAllAsync(roomId);
        return MatrixRoomSummary.From(allRoomEvents);
    }

    internal IQueryable<MatrixEvent> Query<T>(long? since = null)
    {
        var type = MatrixEvent.Types<T>();

        var query = events.Query
            .Where(e => e.Type == type);

        if (since.HasValue)
            query = query.Where(e => e.OriginServerTs > since.Value);

        return query.OrderBy(x => x.OriginServerTs);
    }

    private async Task<string> GetMatrixSenderIdAsync()
    {
        if (MatrixSenderId != null)
            return MatrixSenderId;
        
        var principal = http.HttpContext?.User
            ?? throw new InvalidOperationException("User not authenticated");

        var user = await auth.GetAsync(principal);

        // Ensure the user has a Matrix identity
        var username = user.Avatar.Username.ToLowerInvariant();
        var matrixId = $"@{username}:{Domain}";

        if (!user.HasIdentity("Matrix"))
        {
            user.AddIdentity("Matrix", matrixId);
            await auth.UpdateAsync(user);
        }

        return user.Identity("Matrix")!;
    }
}
