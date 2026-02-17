using Sparc.Blossom.Content;
using Sparc.Blossom.Content.Tovik;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Spaces;

internal class BlossomGameStates(
    BlossomPosts posts,
    IRepository<Quest> questRepo,
    IRepository<Facet> facetRepo,
    IRepository<Constellation> constellationRepo,
    IRepository<Headspace> headspaceRepo)
{
    public async Task ActivateQuestAsync(BlossomSpace space, string facetId, string userId)
    {
        var facet = await facetRepo.FindAsync(space.Id, facetId);
        var headspace = await headspaceRepo.FindAsync(space.Id, userId);

        if (facet == null || headspace == null)
            return;

        headspace.ActiveQuest = new Quest(space, facet!, headspace!.User);
        await headspaceRepo.UpdateAsync(headspace);
    }
    
    public async Task<GameState> GetCoordinatesAsync(BlossomSpace space, string userId)
    {
        var spacePosts = await posts.GetAllAsync(space);
        var spaceFacets = await facetRepo.Query.Where(x => x.SpaceId == space.Id).ToListAsync();
        var spaceConstellations = await constellationRepo.Query.Where(x => x.SpaceId == space.Id).ToListAsync();
        var headspaces = await headspaceRepo.Query.Where(x => x.SpaceId == space.Id).ToListAsync();
        var quests = await questRepo.Query.Where(x => x.SpaceId == space.Id && x.User.Id == userId).ToListAsync();

        var headspace = headspaces
            .Where(x => x.User.Id == userId)
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefault();

        var distanceToAnswer = headspace?.Vector.DistanceTo(space.Vector) ?? 0;
        var axes = headspace?.Axes ?? space.Axes;

        if (headspace != null)
        {
            spaceFacets = spaceFacets.Where(x => x.IsQuestable(space, headspace, distanceToAnswer)).ToList();
            headspace.ActiveQuest?.MaterializeCoordinates(axes);
        }


        spacePosts.ForEach(x => x.MaterializeCoordinates(axes));
        spaceFacets.ForEach(x => x.MaterializeCoordinates(axes));
        spaceConstellations.ForEach(x => x.MaterializeCoordinates(axes));
        headspaces.ForEach(x => x.MaterializeCoordinates(axes));

        return new(space, headspace, spacePosts, headspaces, spaceFacets, spaceConstellations, distanceToAnswer);
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
