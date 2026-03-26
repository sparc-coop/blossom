using MathNet.Numerics.LinearAlgebra;
using Sparc.Blossom.Content;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Spaces;

internal class BlossomSpaceQuests(
    IRepository<Facet> facets,
    IRepository<Quest> quests,
    IRepository<BlossomSpace> spaces,
    BlossomSpaceObjects objects,
    BlossomPosts posts,
    IEnumerable<ITranslator> translators,
    VoyageTranslator vectorizer)
{
    readonly AITranslator translator = translators.OfType<AITranslator>().First();

    public async Task<List<Quest>> FindQuestsAsync(BlossomSpace space, BlossomSpace userSpace)
    {
        var allObjects = await objects.GetAllAsync(space!);

        var newQuests = new List<Quest>();
        var maxAlignment = 0.8f;
        foreach (var obj in allObjects)
        {
            var quest = new Quest(space, userSpace);
            var path = quest.Travel(obj, allObjects, 100, 0.5f, 2f);
            if (!newQuests.Any(x => x.Vector.AlignmentWith(quest.Vector) > maxAlignment))
                newQuests.Add(quest);
        }

        await quests.UpdateAsync(newQuests);
        return newQuests;
    }

    public async Task<List<Facet>> FacetAsync(BlossomSpace space, IEnumerable<Post> facts)
    {
        var postsToFacet = await posts.GetAllAsync(space);

        // Start using the posts from non-system users once there is enough
        if (postsToFacet.Count < 2)
        {
            var initialComponents = ToPrincipalComponents(facts.Select(g => g.Vector), 0.8, 10);
            var facets = initialComponents.Select(c => new Facet(space, c, facts)).ToList();
            space.MaterializeAxes(facets);
            await spaces.UpdateAsync(space);
            return facets;
        }

        // Factor into principal components
        var components = ToPrincipalComponents(postsToFacet.Select(p => p.Vector), 0.8, 10);

        var existingFacets = await facets.Query
            .Where(x => x.SpaceId == space.Id)
            .ToListAsync();

        await facets.DeleteAsync(existingFacets);

        var newFacets = components.Select(x => new Facet(space, x))
            .OrderByDescending(f => f.Vector.CoherenceWeight)
            .ToList();

        //var primaryFacet = newFacets.FirstOrDefault();
        //if (primaryFacet != null)
        //space.CalculateAnswer(primaryFacet, postsToFacet);
        // Make sure the facets are aligned with the newly calculated answer
        newFacets.ForEach(x => x.AlignWith(space));

        space.MaterializeAxes(newFacets);

        await Parallel.ForEachAsync(newFacets, async (childFacet, _) =>
            await SummarizeAsync(childFacet, space));

        await spaces.UpdateAsync(space);

        return newFacets;
    }

    public static List<BlossomVector> ToPrincipalComponents(IEnumerable<BlossomVector> vectors, double varianceToExplain = 1, int maxCount = 3)
    {
        var mean = BlossomVector.Average(vectors);
        var centeredVectors = vectors.Select(v => v.Center(mean)).ToList();
        var matrix = ToMatrix(centeredVectors);

        var svd = matrix.Svd(true);
        var components = new List<BlossomVector>();
        for (int i = 0; i < Math.Min(maxCount, Math.Min(svd.S.Count, svd.VT.RowCount)); i++)
        {
            var componentArray = svd.VT.Row(i).ToArray();

            var component = new BlossomVector(componentArray)
            {
                CoherenceWeight = (float)(Math.Pow(svd.S[i], 2) / svd.S.Sum(x => x * x))
            };
            components.Add(component);

            if (components.Sum(c => c.CoherenceWeight) >= varianceToExplain)
                break;
        }

        return components;
    }

    public async Task<Quest?> ActivateQuestAsync(BlossomSpace space, BlossomSpace userSpace)
    {
        if (userSpace.ActiveQuestId != null)
        {
            await spaces.ExecuteAsync(userSpace, x => x.DeactivateQuest());
            return null;
        }

        var quest = new Quest(space, userSpace);
        await quests.AddAsync(quest);

        await spaces.ExecuteAsync(userSpace, x => x.ActivateQuest(quest));

        //await SeedAsync(space, userSpace, quest, paths);

        return quest;
    }

    internal async Task SummarizeAsync(Facet facet, BlossomSpace space)
    {
        if (space.Vector.PositionOnAxis(facet.Vector) < 0)
            throw new Exception("Facet vector is in the opposite direction of the space vector, cannot summarize.");

        var relevantFacts = await posts.SearchAsync(space, facet.Vector, 20);
        facet.SetSignposts(relevantFacts.Select(x => x.Item));

        var translator = translators.OfType<AITranslator>().First();
        var question = new SummaryQuestion(facet, space.Vector);
        var summary = await translator.AskAsync(question);
        facet.SetSummary(summary.Value);

        await facets.UpdateAsync(facet);
    }

    public async Task<List<Fact>> SeedAsync(BlossomSpace space, BlossomSpace userSpace, Quest quest, List<QuestPath> paths)
    {
        var seed = await translator.AskAsync(new JourneyQuestion(userSpace, paths));

        var facts = seed.Value!.Facts.Select(x => new Fact(space, x)).ToList();
        var questions = seed.Value!.Questions.Select(x => new Question(space, x)).ToList();
        quest.Hint = seed.Value.Hint;

        await vectorizer.VectorizeAsync([.. facts, .. questions]);

        await posts.UpdateAsync(facts);
        await posts.UpdateAsync(questions);

        await quests.UpdateAsync(quest);

        return facts;
    }

    public static Matrix<float> ToMatrix(List<BlossomVector> vectors)
        => Matrix<float>.Build.Dense(vectors.Count, vectors.First().Vector.Length, (i, j) => vectors[i].Vector[j]);

    internal async Task<List<Facet>> GetAllAsync(BlossomSpace space) => await facets.Query.Where(x => x.SpaceId == space.Id).ToListAsync();

    internal async Task<(Quest quest, List<QuestPath> paths)> GetActiveQuestAsync(BlossomSpace userSpace, IEnumerable<BlossomSpaceObject> spaceObjects)
    {
        if (userSpace.ActiveQuestId == null)
            await ActivateQuestAsync(userSpace, userSpace);

        var quest = await quests.FindAsync(userSpace.Id, userSpace.ActiveQuestId!);
        //var paths = await questPaths.Query.Where(x => x.SpaceId == quest!.Id)
        //    .OrderBy(x => x.Index)
        //    .ToListAsync();

        return (quest!, []);
    }
}
