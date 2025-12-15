using Sparc.Blossom.Authentication;
using Sparc.Blossom.Spaces;

namespace Sparc.Blossom.Content;

public class BlossomPost : TextContent
{
    public BlossomPost() : base()
    { }

    public BlossomPost(string domain, string spaceId, Language language, string text, BlossomUser user)
        : base(domain, spaceId, language, text, user)
    { }

    public string PostId { get { return Id; } set { Id = value; } }

    public List<SparcEntity> Entities { get; set; } = [];
    public async Task ExtractEntities(ISparcContent tovik, List<SparcEntityType> entityTypes)
    {
        Entities = await tovik.ExtractGraphAsync(new(this, entityTypes));
    }
}
