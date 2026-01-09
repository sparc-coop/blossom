using Sparc.Blossom.Authentication;
using Sparc.Blossom.Spaces;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Content;

public record LinkedSpace(string SpaceId, double? Distance, double? Alignment)
{
    [JsonConstructor]
    protected LinkedSpace() : this("", null, null)
    { }

    public double Closeness => Distance == null ? 0 : 1 - Distance.Value;
    public double Score => Distance == null || Alignment == null ? 0 : Closeness * Math.Abs(Alignment.Value);
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
    public double X { get; set; } = 0;
    public double CoherenceWeight { get; set; } = 0;

    public async Task ExtractEntities(ISparcContent tovik, List<SparcEntityType> entityTypes)
    {
        Entities = await tovik.ExtractGraphAsync(new(this, entityTypes));
    }

    public void UnlinkAllSpaces() => LinkedSpaces.Clear();
    public bool IsLinked(BlossomSpace space) => LinkedSpaces.Any(x => x.SpaceId == space.SpaceId);
    public LinkedSpace? LinkedSpace(string id) => LinkedSpaces.FirstOrDefault(x => x.SpaceId == id);
    public void LinkToSpace(string spaceId, double? distance, double? alignment)
    {
        LinkedSpaces.RemoveAll(x => x.SpaceId == spaceId);
        LinkedSpaces.Add(new(spaceId, distance, alignment));
    }
}
