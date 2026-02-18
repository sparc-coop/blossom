using MathNet.Numerics.LinearAlgebra;
using Sparc.Blossom.Content;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Spaces;

internal class BlossomSpaceFaceter(
    IRepository<Facet> facets,
    IRepository<Quest> quests,
    BlossomPosts posts,
    IEnumerable<ITranslator> translators)
{
    public async Task<List<Facet>> FacetAsync(BlossomSpace space)
    {
        var postsToFacet = await posts.GetAllAsync(space);
        
        if (postsToFacet.Count() < 2)
            return [];
        
        // Factor into principal components
        var components = ToPrincipalComponents(postsToFacet, 0.8, 10);

        // Match to existing facets when possible (for axis permanence)
        var existingFacets = await facets.Query
            .Where(x => x.SpaceId == space.Id)
            .ToListAsync();

        var newFacets = new List<Facet>();

        foreach (var component in components)
        {
            var bestMatch = existingFacets
                .OrderByDescending(x => x.Vector.AlignmentWith(component))
                .FirstOrDefault();

            if (bestMatch != null)
            {
                // PCA axis may be flipped, so check direction
                if (component.DotProduct(bestMatch.Vector) < 0)
                    component.Vector = component.Multiply(-1).Vector;
                
                bestMatch.Vector = component;
                await facets.UpdateAsync(bestMatch);

                newFacets.Add(bestMatch);
                existingFacets.Remove(bestMatch);
            }
            else
            {
                var newFacet = new Facet(space, component);
                newFacets.Add(newFacet);
            }
        }
        
        // Delete any remaining unused facets
        await facets.DeleteAsync(existingFacets);

        await Parallel.ForEachAsync(newFacets, async (childFacet, _) => 
            await SummarizeAsync(childFacet));

        space.MaterializeAxes(newFacets);

        await facets.UpdateAsync(newFacets);

        return newFacets;
    }

    public static List<BlossomVector> ToPrincipalComponents(IEnumerable<Post> postsToFacet, double varianceToExplain = 1, int maxCount = 3)
    {
        var vectors = postsToFacet.Select(p => p.Vector).ToList();

        var mean = BlossomVector.Average(vectors);
        var centeredVectors = vectors.Select(v => v.Center(mean)).ToList();
        var matrix = ToMatrix(centeredVectors);

        var svd = matrix.Svd(true);
        var components = new List<BlossomVector>();
        for (int i = 0; i < Math.Min(maxCount, Math.Min(svd.S.Count, svd.VT.RowCount)); i++)
        {
            var componentArray = svd.VT.Row(i).ToArray();

            components.Add(new BlossomVector(componentArray)
            {
                Point = mean.Vector,
                CoherenceWeight = (float)(Math.Pow(svd.S[i], 2) / svd.S.Sum(x => x * x))
            });

            if (components.Sum(c => c.CoherenceWeight) >= varianceToExplain)
                break;
        }

        return components;
    }

    async Task SummarizeAsync(Facet facet)
    {
        var translator = translators.OfType<AITranslator>().First();

        var leftPosts = await posts.SearchAsync(facet.SpaceId, facet.Vector, 5, -0.0001);

        var rightPosts = await posts.SearchAsync(facet.SpaceId, facet.Vector, 5);

        var summary = await translator.SummarizeAsync(leftPosts, rightPosts);
        facet.SetSummary(summary);
    }

    public async Task<Quest> ActivateQuestAsync(BlossomSpace space, BlossomSpace userSpace, string facetId)
    {
        var facet = await facets.FindAsync(space.Id, facetId)
            ?? throw new Exception($"Facet with ID {facetId} not found in space {space.Id}");

        var quest = new Quest(userSpace, facet);
        await quests.AddAsync(quest);
        userSpace.ActivateQuest(quest);

        return quest;
    }

    public static Matrix<float> ToMatrix(List<BlossomVector> vectors)
        => Matrix<float>.Build.Dense(vectors.Count, vectors.First().Vector.Length, (i, j) => vectors[i].Vector[j]);

}
