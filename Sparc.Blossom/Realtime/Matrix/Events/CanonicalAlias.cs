namespace Sparc.Blossom.Realtime.Matrix;

public record CanonicalAlias(string? Alias = null, List<string>? AltAliases = null) : IMatrixRoomEvent
{
    public void ApplyTo(MatrixRoomSummary room)
    {
        if (Alias != null)
            room.CanonicalAlias = Alias;
    }
}