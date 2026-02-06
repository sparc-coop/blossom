using Sparc.Blossom.Authentication;
using Sparc.Blossom.Spaces;

namespace Sparc.Blossom.Content;

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

    public BlossomPost(BlossomSpace space, string contentType, string text)
        : base(space.SpaceId, space.Id, Language.Find("en")!, text, BlossomUser.System)
    {
        ContentType = contentType;
    }

    public string PostId { get { return Id; } set { Id = value; } }
    public List<SparcEntity> Entities { get; set; } = [];
    public double CoherenceWeight { get; set; } = 0;
    public string? ConstellationId { get; set; }

    public async Task ExtractEntities(ISparcContent tovik, List<SparcEntityType> entityTypes)
    {
        Entities = await tovik.ExtractGraphAsync(new(this, entityTypes));
    }
}
