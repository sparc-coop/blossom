using Sparc.Blossom.Content;
using Sparc.Blossom.Content.Tovik;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Spaces;

internal class BlossomSpaceObjects(
    BlossomPosts posts,
    IRepository<BlossomSpace> spaces,
    IRepository<Fact> facts,
    IRepository<Question> questions,
    IRepository<BlossomSpaceObject> allObjects)
{
    public async Task<List<BlossomSpaceObject>> GetAllAsync(BlossomSpace space)
    {
        var spaceFacts = await facts.Query.Where(x => x.SpaceId == space.Id).ToListAsync();
        var spaceQuestions = await questions.Query.Where(x => x.SpaceId == space.Id).ToListAsync();
        var spacePosts = await posts.GetAllAsync(space, 10000);
        var users = await spaces.Query.Where(x => x.SpaceId == space.Id && x.RoomType == "User").ToListAsync();

        return [.. spaceFacts, .. spaceQuestions, .. spacePosts, ..users];
    }

    public async Task UpdateAsync(IEnumerable<BlossomSpaceObject> objects)
    {
        var factsToUpdate = objects.OfType<Fact>();
        var questionsToUpdate = objects.OfType<Question>();
        var postsToUpdate = objects.OfType<Post>();
        var usersToUpdate = objects.OfType<BlossomSpace>().Where(x => x.RoomType == "User");

        await facts.UpdateAsync(factsToUpdate);
        await questions.UpdateAsync(questionsToUpdate);
        await posts.UpdateAsync(postsToUpdate);
        await spaces.UpdateAsync(usersToUpdate);
    }

    internal async Task RecalculateAsync(BlossomSpace space)
    {
        var objects = await GetAllAsync(space);
        objects.ForEach(x => x.SetGravitationalForce(objects));
        await UpdateAsync(objects);

        //var hintVectors = await vectors.GetAllAsync(space.Space.Id, "Hint");
        //var hint = await vectors.CalculateHintAsync(userSpace, post, space);
        //await posts.AddAsync(hint);
    }


    public record GraphExtractionResult(List<SparcEntityBase> Entities, List<SparcRelationship> Relationships);
    public static async Task<List<SparcEntity>> ExtractGraph(ExtractGraphRequest request)
    {
        //var options = new TranslationOptions
        //{
        //    Instructions = SparcPrompts.GraphExtraction(request.EntityTypes),
        //    Schema = new BlossomSchema(typeof(GraphExtractionResult))
        //};

        //var graph = await contents.TranslateAsync<GraphExtractionResult>(request.Content, options);
        //if (graph?.Entities == null)
        return [];

        //var entities = graph.Entities.Select(x => new SparcEntity(x, graph.Relationships));
        //return entities.ToList();
    }

    internal async Task DeleteAsync(string spaceId)
    {
        var allVectors = await allObjects.Query.Where(x => x.SpaceId == spaceId).ToListAsync();
        await allObjects.DeleteAsync(allVectors);
    }
}
