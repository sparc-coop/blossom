using Sparc.Blossom.Content;

namespace Sparc.Blossom.Spaces;

public class BlossomSpaceFaceter(BlossomVectors vectors)
{
    public async Task<List<BlossomVector>> FacetAsync(IEnumerable<BlossomVector> vectorsToFacet)
    {
        if (vectorsToFacet.Count() < 2)
            return [];
        
        // Factor into principal components
        var facets = ToPrincipalComponents(vectorsToFacet, 1, 3);

        // Match to existing facets when possible (for axis permanence)
        var existingFacets = await vectors.GetAllAsync(vectorsToFacet.First().SpaceId, "Facet");
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

    public static List<BlossomVector> ToPrincipalComponents(IEnumerable<BlossomVector> vectors, double varianceToExplain = 1, int maxCount = 3)
    {
        var mean = BlossomVector.Average(vectors);
        var centeredVectors = vectors.Select(v => v.Center(mean)).ToList();
        var matrix = BlossomVector.ToMatrix(centeredVectors);

        var svd = matrix.Svd(true);
        var components = new List<BlossomVector>();
        for (int i = 0; i < Math.Min(maxCount, Math.Min(svd.S.Count, svd.VT.RowCount)); i++)
        {
            var componentArray = svd.VT.Row(i).ToArray();

            components.Add(new BlossomVector(vectors.First().SpaceId, "Facet", Guid.NewGuid().ToString(), componentArray)
            {
                Point = mean.Vector,
                CoherenceWeight = Math.Pow(svd.S[i], 2) / svd.S.Sum(x => x * x)
            });

            if (components.Sum(c => c.CoherenceWeight) >= varianceToExplain)
                break;
        }

        return components;
    }
}
