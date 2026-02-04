using Sparc.Blossom.Content;

namespace Sparc.Blossom.Spaces;

public class BlossomSpaceFaceter(BlossomVectors vectors)
{
    public async Task<List<BlossomVector>> FacetAsync(BlossomSpaceWithVector space, List<BlossomPostWithVector> posts)
    {
        if (posts.Count < 2)
            return [];
        
        // Factor into principal components
        var facets = BlossomVector.ToPrincipalComponents(posts.Select(x => x.Vector), 1, 3);

        // Match to existing facets when possible (for axis permanence)
        var existingFacets = await vectors.GetAllAsync(space.Space, "Facet");
        foreach (var facet in facets)
        {
            var bestMatch = existingFacets
                .OrderByDescending(x => x.AlignmentWith(facet))
                .FirstOrDefault();

            if (bestMatch != null)
            {
                // PCA axis may be flipped, so check direction
                if (facet.DotProduct(bestMatch) < 0)
                    facet.Vector = facet.Multiply(-1).Vector;
                facet.Id = bestMatch.Id;
                existingFacets.Remove(bestMatch);
            }

            await vectors.UpdateAsync(facet);
        }
        
        // Delete any remaining unused facets
        await vectors.DeleteAsync(existingFacets);

        await Parallel.ForEachAsync(facets, async (childFacet, _) => 
            await vectors.SummarizeAsync(childFacet));

        return facets;
    }
}
