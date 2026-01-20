using Sparc.Blossom.Authentication;
using Sparc.Blossom.Spaces;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Content;

public record LinkedSpace(string SpaceId, string Name, string Type, double X, double Y)
{
    public LinkedSpace(BlossomSpace space, double x, double y)
        : this(space.Id, string.IsNullOrWhiteSpace(space.Name) ? space.Id : space.Name, space.RoomType, x, y)
    {
    }

    [JsonConstructor]
    protected LinkedSpace() : this("", "", "", 0, 0)
    { }
};

public class BlossomPost : TextContent
{
    public BlossomPost() : base()
    {
        ContentType = "Post";
    }

    public BlossomPost(string domain, string spaceId, Language language, string text, BlossomUser user)
        : base(domain, spaceId, language, text, user)
    {
        ContentType = "Post";
    }

    public string PostId { get { return Id; } set { Id = value; } }
    public List<LinkedSpace> LinkedSpaces { get; set; } = [];
    public List<SparcEntity> Entities { get; set; } = [];
    public double CoherenceWeight { get; set; } = 0;

    public async Task ExtractEntities(ISparcContent tovik, List<SparcEntityType> entityTypes)
    {
        Entities = await tovik.ExtractGraphAsync(new(this, entityTypes));
    }

    public bool IsLinked(BlossomSpace space) => LinkedSpaces.Any(x => x.SpaceId == space.SpaceId);
    public LinkedSpace? LinkedSpace(string id) => LinkedSpaces.FirstOrDefault(x => x.SpaceId == id);

    public void LinkToSpace(BlossomSpace space, double x, double y)
    {
        LinkedSpaces.RemoveAll(x => x.SpaceId == space.Id);
        LinkedSpaces.Add(new(space, x, y) { Name = Id });
    }

    public void ClearLinks(string type)
    {
        LinkedSpaces.RemoveAll(x => x.Type == type);
    }
}
