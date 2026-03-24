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
    public virtual float Mass => 0;
    public float Temperature { get; set; }
    public float Luminosity { get; set; }
    public BlossomVector? GravitationalForce { get; set; }
    public float CollapseScale { get; set; }

    public virtual void SetSummary(BlossomSummary? summary)
    {
        Summary = summary;
    }

    protected const float parsecsPerUnit = 11f * 3.262f; // Average size of a stellar nursery * parsecs per light year
    
    public virtual void MaterializeCoordinates(List<Axis> axes) => MaterializeCoordinates(axes, Vector);

    public void MaterializeCoordinates(List<Axis> axes, BlossomVector coordinateVector)
    {
        Coordinates = coordinateVector.ToCoordinates(axes, GravitationalForce);
        Distance = axes.FirstOrDefault(x => x.Name == "User")?.Vector.AngularDistanceTo(coordinateVector, parsecsPerUnit) ?? 0;
    }
    
    protected const float gravitationalConstant = 1;
    public virtual void SetGravitationalForce(IEnumerable<BlossomSpaceObject> objects)
    {
        if (Mass == 0)
            return;
        
        var forces = objects.Where(x => x.Id != Id && x.Mass > 0)
            .Select(x => x.Vector.Subtract(Vector).Normalize().Multiply(GravitationalScale(x)))
            .ToList();

        GravitationalForce = BlossomVector.Sum(forces);
        CollapseScale = GravitationalForce.Magnitude();
    }

    float GravitationalScale(BlossomSpaceObject other)
    {
        var distance = Vector.AngularDistanceTo(other.Vector, parsecsPerUnit);
        if (distance == 0)
            return 0;

        return gravitationalConstant * Mass * other.Mass / (distance * distance);
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
