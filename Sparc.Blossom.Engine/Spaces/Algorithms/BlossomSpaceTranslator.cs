using Sparc.Blossom.Authentication;
using Sparc.Blossom.Content;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Spaces;

internal class BlossomSpaceTranslator
    (IEnumerable<ITranslator> translators,
    BlossomPosts posts,
    IRepository<BlossomSpace> spaces,
    IRepository<BlossomUserTrail> headspaces,
    BlossomSpaceFacets facets,
    BlossomSpaceConstellations constellations,
    VoyageTranslator vectorizer,
    FriendlyId friendlyId)
{
    readonly AITranslator translator = translators.OfType<AITranslator>().First();

    public async Task<List<Fact>> SeedAsync(BlossomSpace space, Post question, int count)
    {
        var seed = await translator.AskAsync(new SpaceDiscoveryQuestion(space, question, count));

        var facts = seed.Value!.Facts.Select(x => new Fact(space, x)).ToList();
        var questions = seed.Value!.Questions.Select(x => new Question(space, x)).ToList();
        
        List<IVectorizable> itemsToVectorize = [.. facts, .. questions];
        if (space.Vector.IsEmpty)
        {
            space.Vector.Text = seed.Value.InitialAnswer;
            itemsToVectorize.Add(space);
        }

        await vectorizer.VectorizeAsync(itemsToVectorize);

        await posts.UpdateAsync(facts);
        await posts.UpdateAsync(questions);

        space.SetSummary(new(friendlyId.Create(), question.Text ?? "", ""));

        await spaces.UpdateAsync(space);
        return facts;
    }

    internal async Task<Post> CalculateHintAsync(BlossomSpace currentLocation, Post lastPost, BlossomSpace destination)
    {
        var journey = destination.Vector.Subtract(currentLocation.Vector);

        var clues = await posts.SearchAsync(journey, 5);
        var question = new AnswerHintQuestion(destination, lastPost, clues);
        var hint = await translator.AskAsync(question);
        var hintPost = new Post(destination, BlossomUser.System.Avatar, hint.Value!.Text);

        await vectorizer.VectorizeAsync(hintPost);
        await posts.UpdateAsync([hintPost]);

        return hintPost;
    }

    internal async Task CalculateAsync(BlossomSpace space, BlossomSpace userSpace)
    {
        var userPosts = await posts.GetAllAsync(space, userSpace.User, 50);
        var post = userPosts.First();

        var headspace = userSpace.Add(post, userPosts.Skip(1).FirstOrDefault(), space);
        await spaces.UpdateAsync(userSpace);
        await headspaces.AddAsync(headspace);

        var activeQuest = await facets.GetActiveQuestAsync(userSpace);
        if (activeQuest != null)
        {
            await facets.SeedAsync(space, userSpace, activeQuest);
        }
        else
        {
            var facts = await SeedAsync(space, post, userPosts.Count == 1 ? 20 : 10);
            await facets.FacetAsync(space, facts);
            //await constellator.ConstellateAsync(space);
        }

        var relevantFacts = await posts.SearchAsync(space.Vector, 20);
        await CalculateAnswerAsync(space, [userPosts.Last(), .. relevantFacts.Select(x => x.Item)]);
        await CalculateAnswerAsync(userSpace, userPosts);

        //var hintVectors = await vectors.GetAllAsync(space.Space.Id, "Hint");
        //var hint = await vectors.CalculateHintAsync(userSpace, post, space);
        //await posts.AddAsync(hint);
    }

    internal async Task RecalculateSpaceAsync(BlossomSpace space, BlossomSpace userSpace)
    {
        var userPosts = await posts.GetAllAsync(space, userSpace.User, 20);
        var post = userPosts.First();

        var headspace = userSpace.Add(post, userPosts.Skip(1).FirstOrDefault(), space);
        await spaces.UpdateAsync(userSpace);
        await headspaces.AddAsync(headspace);

        var activeQuest = await facets.GetActiveQuestAsync(userSpace);
        if (activeQuest != null)
        {
            await facets.SeedAsync(space, userSpace, activeQuest);
        }
        else
        {
            var facts = await SeedAsync(space, post, userPosts.Count == 1 ? 20 : 10);
            await facets.FacetAsync(space, facts);
            //await constellator.ConstellateAsync(space);
        }

        var relevantFacts = await posts.SearchAsync(space.Vector, 20);
        await CalculateAnswerAsync(space, [userPosts.Last(), .. relevantFacts.Select(x => x.Item)]);
        await CalculateAnswerAsync(userSpace, userPosts);

        //var hintVectors = await vectors.GetAllAsync(space.Space.Id, "Hint");
        //var hint = await vectors.CalculateHintAsync(userSpace, post, space);
        //await posts.AddAsync(hint);
    }

    internal async Task CalculateAnswerAsync(BlossomSpace space, IEnumerable<Post> allPosts)
    {
        var summary = await translator.AskAsync(new SummaryQuestion(allPosts, 32000));
        if (summary == null)
            return;

        summary.Value!.Vector.Text = summary.Value.Description;
        await vectorizer.VectorizeAsync(summary.Value);
        
        space.SetSummary(summary.Value);
        space.CalculateAnswer(allPosts);

        await spaces.UpdateAsync(space);
    }

    public async Task<GameState> GetCoordinatesAsync(BlossomSpace space, BlossomSpace userSpace)
    {
        var spacePosts = await posts.GetAllAsync(space);
        var spaceFacets = await facets.GetAllAsync(space);
        var spaceConstellations = await constellations.GetAllAsync(space);

        var userTrails = await headspaces.Query.Where(x => x.SpaceId == space.Id).OrderBy(x => x.Timestamp).ToListAsync();
        var activeQuest = await facets.GetActiveQuestAsync(userSpace);

        var guides = await posts.SearchAsync(userSpace.Vector, 20);
        spacePosts.AddRange(guides.Select(x => x.Item));

        var axes = userSpace.Axes.Count > 0 ? userSpace.Axes.ToList() : space.Axes.ToList();
        axes.Add(new("User", userSpace)); // Z axis is the user space itself, to brighten/dim objects based on user proximity

        List<Quest> availableQuests = activeQuest != null ? [activeQuest] : [];

        if (activeQuest == null || activeQuest.IsExitable(userSpace))
            availableQuests.AddRange(spaceFacets
            .Select(x => new Quest(space, userSpace, x))
            .OrderByDescending(x => x.Importance)
            .ToList());

        List<BlossomSpaceObject> all = [userSpace, space, .. spacePosts, .. userTrails, .. availableQuests, .. spaceConstellations];
        all.ForEach(x => x.MaterializeCoordinates(axes, all));

        spacePosts = spacePosts.OrderBy(x => x.Distance).ToList();

        return new(activeQuest ?? space, userSpace, space, spacePosts, userTrails, availableQuests, spaceConstellations);
    }
}
