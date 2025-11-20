using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Realtime;

public class MatrixEvents(
    IRepository<BlossomEvent> events,
    IHttpContextAccessor http,
    SparcAuthenticator<BlossomUser> auth)
{
    public const string Domain = "sparc.coop";
    public string? MatrixSenderId;

    public async Task<BlossomEvent> PublishAsync<T>(string roomId, T content)
    {
        var sender = await GetMatrixSenderIdAsync();

        var ev = BlossomEvent.Create(roomId, sender, content);
        await events.AddAsync(ev);
        return ev;
    }

    internal async Task<List<BlossomEvent>> GetAllAsync(string roomId)
    {
        return await events.Query
            .Where(x => x.RoomId == roomId)
            .OrderBy(x => x.Depth)
            .ToListAsync();
    }

    internal async Task<List<BlossomEvent<T>>> GetAllAsync<T>(string roomId)
    {
        var type = BlossomEvent.Types<T>();
        var result = await events.Query
            .Where(e => e.RoomId == roomId && e.Type == type)
            .OrderBy(x => x.Depth)
            .ToListAsync();

        return result.Cast<BlossomEvent<T>>().ToList();
    }

    internal async Task<MatrixRoom> GetRoomAsync(string roomId)
    {
        var allRoomEvents = await GetAllAsync(roomId);
        return MatrixRoom.From(allRoomEvents);
    }

    internal IQueryable<BlossomEvent> Query<T>()
    {
        var type = BlossomEvent.Types<T>();
        return events.Query
            .Where(e => e.Type == type)
            .OrderBy(x => x.Depth);
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
