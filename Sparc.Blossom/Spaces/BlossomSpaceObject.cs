using Sparc.Blossom.Authentication;
using Sparc.Blossom.Content;

namespace Sparc.Blossom.Spaces;

public class BlossomSpaceObject(string spaceId) : BlossomEntity<string>(Guid.NewGuid().ToString()), IVectorizable
{
    public BlossomSpaceObject(BlossomSpace space, BlossomVector? vector = null)
        : this(space.Id)
    {
        Vector = vector ?? new BlossomVector();
    }

    public string SpaceId { get; set; } = spaceId;
    public BlossomVector Vector { get; set; } = new();
    public BlossomSummary? Summary { get; set; }
    public BlossomAvatar User { get; set; } = BlossomUser.System.Avatar;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public BlossomVector? Coordinates { get; set; }

    public virtual void SetSummary(BlossomSummary? summary)
    {
        Summary = summary;
    }

    public virtual void MaterializeCoordinates(List<Axis> axes)
    {
        Coordinates = Vector.ToCoordinates(axes);
    }
}
