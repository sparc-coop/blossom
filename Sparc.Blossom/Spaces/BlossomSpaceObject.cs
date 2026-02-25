using Sparc.Blossom.Authentication;
using Sparc.Blossom.Content;
using System.Text.Json.Serialization.Metadata;

namespace Sparc.Blossom.Spaces;

public class BlossomSpaceObject(string spaceId) : BlossomEntity<string>(Guid.NewGuid().ToString()), IVectorizable
{
    public BlossomSpaceObject() : this("")
    { }
    
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
    public float Distance { get; set; }

    public virtual void SetSummary(BlossomSummary? summary)
    {
        Summary = summary;
    }

    protected const float lightYearsPerUnit = 46_500_000_000;
    
    public virtual void MaterializeCoordinates(List<Axis> axes) => MaterializeCoordinates(axes, Vector);

    public void MaterializeCoordinates(List<Axis> axes, BlossomVector coordinateVector)
    {
        Coordinates = coordinateVector.ToCoordinates(axes);
        Distance = axes.FirstOrDefault(x => x.Name == "User")?.Vector.AngularDistanceTo(coordinateVector, lightYearsPerUnit) ?? 0;
    }

    public static void DoNotSerializeVectors(JsonTypeInfo typeInfo)
    {
        if (!typeInfo.Type.IsAssignableTo(typeof(BlossomSpaceObject)))
            return;

        foreach (var prop in typeInfo.Properties)
        {
            if (prop.PropertyType == typeof(BlossomVector) && !prop.Name.Contains("coordinates"))
                prop.ShouldSerialize = (_, _) => false;
        }
    }

}
