using MathNet.Numerics.LinearAlgebra;
using Sparc.Blossom.Content;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Spaces;

internal class BlossomSpaceFaceter(
    IRepository<Facet> facets,
    IRepository<Quest> quests,
    IRepository<BlossomSpace> spaces,
    BlossomPosts posts,
    IEnumerable<ITranslator> translators)
{
    public async Task SeedAsync(BlossomSpace space, IEnumerable<Fact> guides)
    {
        var components = ToPrincipalComponents(guides.Select(g => g.Vector), space.Vector, 0.8, 10);
        var facets = components.Select(c => new Facet(space, c, guides)).ToList();
        space.MaterializeAxes(facets);
        await spaces.UpdateAsync(space);
    }
    
    public async Task<List<Facet>> FacetAsync(BlossomSpace space)
    {
        var postsToFacet = await posts.GetAllAsync(space);

        // Start using the posts from non-system users once there is enough
        if (postsToFacet.Count < 2)
            return [];

        // Factor into principal components
        var components = ToPrincipalComponents(postsToFacet.Select(p => p.Vector), space.Vector, 0.8, 10);

        var existingFacets = await facets.Query
            .Where(x => x.SpaceId == space.Id)
            .ToListAsync();

        await facets.DeleteAsync(existingFacets);
        
        var newFacets = components.Select(x => new Facet(space, x, postsToFacet))
            .OrderByDescending(x => x.Vector.CoherenceWeight)
            .ToList();

        //var primaryFacet = newFacets.FirstOrDefault();
        //if (primaryFacet != null)
            //space.CalculateAnswer(primaryFacet, postsToFacet);
        // Make sure the facets are aligned with the newly calculated answer
        newFacets.ForEach(x => x.AlignWith(space));

        space.MaterializeAxes(newFacets);

        await Parallel.ForEachAsync(newFacets, async (childFacet, _) =>
            await SummarizeAsync(childFacet, space));

        await facets.UpdateAsync(newFacets);

        return newFacets;
    }

    public static List<BlossomVector> ToPrincipalComponents(IEnumerable<BlossomVector> vectors, BlossomVector alignmentVector, double varianceToExplain = 1, int maxCount = 3)
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

    async Task SummarizeAsync(Facet facet, BlossomSpace space)
    {
        if (space.Vector.PositionOnAxis(facet.Vector) < 0)
            throw new Exception("Facet vector is in the opposite direction of the space vector, cannot summarize.");

        var translator = translators.OfType<AITranslator>().First();
        var question = new SummaryQuestion(facet, space.Vector);
        var summary = await translator.AskAsync(question);
        facet.SetSummary(summary.Value);
    }

    public async Task<Quest> ActivateQuestAsync(BlossomSpace space, BlossomSpace userSpace, string facetId)
    {
        var facet = await facets.FindAsync(space.Id, facetId)
            ?? throw new Exception($"Facet with ID {facetId} not found in space {space.Id}");

        var quest = new Quest(space, userSpace, facet);
        await quests.AddAsync(quest);
        userSpace.ActivateQuest(quest);

        return quest;
    }

    public static Matrix<float> ToMatrix(List<BlossomVector> vectors)
        => Matrix<float>.Build.Dense(vectors.Count, vectors.First().Vector.Length, (i, j) => vectors[i].Vector[j]);

}
