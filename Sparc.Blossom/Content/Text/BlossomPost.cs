using Sparc.Blossom.Authentication;
using Sparc.Blossom.Spaces;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Content;

public record LinkedSpace(string SpaceId, double? Distance, double? Alignment)
{
    [JsonConstructor]
    protected LinkedSpace() : this("", null, null)
    { }
    
    public LinkedSpace(BlossomVector postVector, BlossomVector spaceVector)
        : this(spaceVector.SpaceId,
            spaceVector.DistanceTo(postVector),
            spaceVector.SimilarityTo(postVector))
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

    public async Task ExtractEntities(ISparcContent tovik, List<SparcEntityType> entityTypes)
    {
        Entities = await tovik.ExtractGraphAsync(new(this, entityTypes));
    }

    public void UnlinkAllSpaces() => LinkedSpaces.Clear();
    public LinkedSpace? LinkedSpace(string id) => LinkedSpaces.FirstOrDefault(x => x.SpaceId == id);
    public void LinkToSpace(BlossomVector postVector, BlossomVector spaceVector)
    {
        LinkedSpaces.RemoveAll(x => x.SpaceId == spaceVector.SpaceId);
        LinkedSpaces.Add(new(postVector, spaceVector));
    }
}
