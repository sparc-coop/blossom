using Sparc.Blossom.Spaces;

namespace Sparc.Blossom.Content.Tovik;

public static class SparcPrompts
{
    public static string GraphExtraction(List<SparcEntityType> entityTypes) => $@"
-Goal-
Given the user supplied text and the following list of entity types, identify all entities of those types from the text and all relationships among the identified entities.
You will produce two lists: one list of entities and one list of relationships among those entities.

-Constraints-
You MUST produce at least one entity if any entity type is mentioned in the text. 
You MUST produce at least one relationship if any two entities are clearly related to each other in the text.
All relationships must be between two entities that actually appear in the entities list.
All entity types must be one of the given entity types.
All relationships must contain a weight between 0 and 10, 10 being the strongest.

-Entity Types-
{List(entityTypes, e => $"{e.Name}: {e.Description}")}

-Steps-
1. Identify all entities. Extract the information given in the schema, following the embedded schema descriptions. Put them in the entities list.
2. From the entities identified in step 1, identify all pairs of (SourceEntityName, TargetEntityname) that are *clearly related* to each other. Put them in the relationships list. 
";

    static string List<T>(List<T> list, Func<T, string> format) => string.Join("\r\n", list.Select(format));
}
