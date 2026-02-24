using Sparc.Blossom.Content;
using Sparc.Blossom.Content.Tovik;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Spaces;

internal class BlossomGameStates(
    BlossomPosts posts,
    IRepository<Facet> facetRepo,
    IRepository<Quest> questRepo,
    IRepository<Constellation> constellationRepo,
    IRepository<BlossomUserTrail> userTrailRepo)
{
    public async Task<GameState> GetCoordinatesAsync(BlossomSpace space, BlossomSpace userSpace)
    {
        var spacePosts = await posts.GetAllAsync(space);
        var spaceFacets = await facetRepo.Query.Where(x => x.SpaceId == space.Id).ToListAsync();
        var spaceConstellations = await constellationRepo.Query.Where(x => x.SpaceId == space.Id).ToListAsync();
        var userTrails = await userTrailRepo.Query.Where(x => x.SpaceId == space.Id).OrderBy(x => x.Timestamp).ToListAsync();
        var activeQuest = userSpace.ActiveQuestId == null
            ? null
            : await questRepo.FindAsync(space.Id, userSpace.ActiveQuestId);

        var guides = await posts.SearchAsync(userSpace.Vector, 20);
        spacePosts.AddRange(guides.Select(x => x.Item));

        var distanceToAnswer = activeQuest == null
            ? userSpace.Origin.DistanceTo(space.Vector)
            : userSpace.Origin.DistanceTo(activeQuest.Vector);

        var axes = userSpace.Axes.Count > 0 ? userSpace.Axes.ToList() : space.Axes.ToList();
        axes.Add(new("User", userSpace)); // Z axis is the user space itself, to brighten/dim objects based on user proximity

        var availableQuests = activeQuest != null 
            ? [activeQuest]
            : spaceFacets
            .Select(x => new Quest(space, userSpace, x))
            .OrderByDescending(x => x.Importance)
            .ToList();

        List<BlossomSpaceObject> all = [userSpace, space, .. spacePosts, .. userTrails, .. availableQuests, .. spaceConstellations];
        all.ForEach(x => x.MaterializeCoordinates(axes));

        spacePosts = spacePosts.OrderBy(x => x.Distance).ToList();

        return new(activeQuest ?? space, userSpace, space, spacePosts, userTrails, availableQuests, spaceConstellations);
    }

    public record GraphExtractionResult(List<SparcEntityBase> Entities, List<SparcRelationship> Relationships);
    public async Task<List<SparcEntity>> ExtractGraph(ExtractGraphRequest request)
    {
        var options = new TranslationOptions
        {
            Instructions = SparcPrompts.GraphExtraction(request.EntityTypes),
            Schema = new BlossomSchema(typeof(GraphExtractionResult))
        };

        //var graph = await contents.TranslateAsync<GraphExtractionResult>(request.Content, options);
        //if (graph?.Entities == null)
        return [];

        //var entities = graph.Entities.Select(x => new SparcEntity(x, graph.Relationships));
        //return entities.ToList();
    }
}
