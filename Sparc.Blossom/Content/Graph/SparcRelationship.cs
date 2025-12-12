using System.ComponentModel;

namespace Sparc.Blossom.Spaces;

public class SparcRelationship
{
    [Description("The name of the source entity, as identified in the entity extraction")]
    public string SourceEntityName { get; set; } = "";
    [Description("The name of the target entity, as identified in the entity extraction")]
    public string TargetEntityName { get; set; } = "";
    [Description("Explanation as to why you think the source entity and the target entity are related to each other")]
    public string Description { get; set; } = "";
    [Description("A numeric score indicating strength of the relationship between the source entity and target entity.")]
    public decimal? Weight { get; set; }
}
