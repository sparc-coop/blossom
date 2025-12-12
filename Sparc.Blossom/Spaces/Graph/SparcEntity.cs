using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Spaces;

public record SparcEntityType(string Name, string Description);

public class SparcEntityBase
{
    [Description("Name of the entity, lowercased")]
    public string Name { get; set; } = "";
    [Description("One of the given entity types")]
    public string Type { get; set; } = "";
    [Description("Comprehensive description of the entity's attributes and activities")]
    public string Description { get; set; } = "";
}

public class SparcEntity : BlossomEntity<string>
{
    [JsonConstructor]
    public SparcEntity()
    { }
    
    public SparcEntity(SparcEntityBase entity, List<SparcRelationship>? relationships)
    {
        Name = entity.Name;
        Type = entity.Type;
        Description = entity.Description;
        if (relationships != null)
            Relationships = relationships.Where(x => x.SourceEntityName == entity.Name).ToList();
    }

    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public List<SparcRelationship> Relationships { get; set; } = [];
}
