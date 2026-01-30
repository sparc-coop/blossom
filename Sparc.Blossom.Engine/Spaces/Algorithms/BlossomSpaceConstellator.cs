using Sparc.Blossom.Content;

namespace Sparc.Blossom.Spaces;

public class BlossomSpaceConstellator(BlossomVectors vectors, IRepository<BlossomPost> postRepository)
{
    public async Task<List<BlossomVector>> ConstellateAsync(BlossomSpace rootSpace, List<BlossomPostWithVector> posts, List<BlossomVector> axes)
    {
        if (posts.Count < 3)
            return [];

        var coords = posts.Select(p => p.Vector.ProjectOntoAxes(axes)).ToList();

        var edges = ComputeKnnEdges(coords, rootSpace.Settings.ConstellationStrength);
        var constellations = Kruskal(coords, edges, rootSpace.Settings.ConstellationThreshold);
        var constellationVectors = await CreateConstellations(rootSpace, posts, constellations);

        await vectors.UpdateAsync(posts.Select(p => p.Vector));
        await postRepository.UpdateAsync(posts.Select(p => p.Post));

        await Parallel.ForEachAsync(constellationVectors, async (vec, _) =>
            await vectors.SummarizeAsync(vec));

        return constellationVectors;
    }

    private async Task<List<BlossomVector>> CreateConstellations(BlossomSpace rootSpace, List<BlossomPostWithVector> posts, Dictionary<BlossomVector, List<BlossomVector>> constellations)
    {
        // Create constellation vectors as simple average of coordinates per component
        await vectors.ClearAsync(rootSpace.Id, "Constellation");
        posts.ForEach(x =>
        {
            x.Vector.ConstellationId = null;
            x.Vector.ConstellationConnectorId = null;
        });

        var result = new List<BlossomVector>();
        foreach (var root in constellations.Keys)
        {
            var constellation = constellations[root];
            var centroid = BlossomVector.Average(constellation);
            var constellationVector = new BlossomVector(rootSpace.Id, "Constellation", Guid.NewGuid().ToString(), centroid.Vector);
            result.Add(constellationVector);

            for (var i = 0; i < constellation.Count; i++)
            {
                var post = posts.First(y => y.Post.Id == constellation[i].Id);
                post.Vector.ConstellationId = constellationVector.Id;
                post.Post.ConstellationId = constellationVector.Id;
                if (i < constellation.Count - 1)
                    post.Vector.ConstellationConnectorId = constellation[i + 1].Id;
            }
        }

        await vectors.UpdateAsync(result);
        return result;
    }

    record KnnEdge(BlossomVector Coordinate1, BlossomVector Coordinate2, double Distance);
    static List<KnnEdge> ComputeKnnEdges(List<BlossomVector> coords, int k)
    {
        var edges = new List<KnnEdge>();
        var count = coords.Count;
        
        for (int i = 0; i < count; i++)
        {
            var distances = new List<(int idx, double distance)>();
            for (int j = 0; j < count; j++)
            {
                if (i == j) continue;
                var distance = coords[i].DistanceTo(coords[j]) ?? double.MaxValue;
                distances.Add((j, distance));
            }

            k = Math.Max(1, Math.Min(k, coords.Count - 1));
            foreach (var (idx, distance) in distances.OrderBy(x => x.distance).Take(k))
            {
                var coordinate1 = coords[Math.Min(i, idx)];
                var coordinate2 = coords[Math.Max(i, idx)];
                if (!edges.Any(e => e.Coordinate1 == coordinate1 && e.Coordinate2 == coordinate2))
                    edges.Add(new(coordinate1, coordinate2, distance));
            }
        }

        return edges.OrderBy(e => e.Distance).ToList();
    }

    static Dictionary<BlossomVector, List<BlossomVector>> Kruskal(List<BlossomVector> coords, List<KnnEdge> edges, double threshold)
    {
        var n = coords.Count;
        var parent = new int[n];
        for (int i = 0; i < n; i++) parent[i] = i;

        int Find(int x) => parent[x] == x ? x : (parent[x] = Find(parent[x]));
        void Union(int x, int y) { parent[Find(y)] = Find(x); }

        // Build MST using Kruskal over provided candidate edges
        var mstEdges = new List<(int a, int b, double dist)>();
        foreach (var e in edges.OrderBy(e => e.Distance))
        {
            int a = coords.IndexOf(e.Coordinate1);
            int b = coords.IndexOf(e.Coordinate2);
            if (a < 0 || b < 0) continue;
            if (Find(a) != Find(b))
            {
                Union(a, b);
                mstEdges.Add((a, b, e.Distance));
                if (mstEdges.Count == n - 1) break;
            }
        }

        // If no edges in MST, return each node as its own component
        if (mstEdges.Count == 0)
            return [];

        // Determine cut threshold by finding the largest gap in MST edge weights
        //double threshold = mstEdges.Select(x => x.dist).Median() * 2;
        //double threshold = FindMstCutThreshold(mstEdges);

        // Rebuild unions only for MST edges that are <= threshold (keeps small edges)
        for (int i = 0; i < n; i++) parent[i] = i;
        foreach (var (a, b, dist) in mstEdges)
        {
            if (dist <= threshold)
                Union(a, b);
        }

        // Collect components mapped by root index
        var constellations = new Dictionary<BlossomVector, List<BlossomVector>>();
        for (int i = 0; i < n; i++)
        {
            int r = Find(i);
            if (!constellations.ContainsKey(coords[r])) constellations[coords[r]] = [];
            constellations[coords[r]].Add(coords[i]);
        }

        // Throw out constellations with less than 3 nodes
        foreach (var root in constellations.Keys.ToList())
        {
            if (constellations[root].Count < 3)
                constellations.Remove(root);
        }

        return constellations;
    }

    // Improved MST cut threshold:
    // - Looks for the largest gap in sorted MST edge weights but requires the gap to be
    //   significant either in absolute terms or as a ratio (relative jump). If the gap
    //   is significant, use the midpoint between the two weights as the cut.
    // - If no significant gap is found, fall back to a conservative percentile (75th)
    //   to avoid connecting well-separated clusters via a few long edges.
    static double FindMstCutThreshold(List<(int a, int b, double dist)> mstEdges)
    {
        var weights = mstEdges.Select(e => e.dist).OrderBy(d => d).ToList();
        if (weights.Count == 0) return double.MaxValue;
        if (weights.Count == 1) return weights[0];

        double maxDiff = 0;
        int maxIdx = 0;
        double maxRatio = 0;

        for (int i = 1; i < weights.Count; i++)
        {
            var prev = weights[i - 1];
            var curr = weights[i];
            var diff = curr - prev;
            var ratio = prev > 0 ? curr / prev : double.PositiveInfinity;

            if (diff > maxDiff)
            {
                maxDiff = diff;
                maxIdx = i;
            }
            if (ratio > maxRatio)
            {
                maxRatio = ratio;
            }
        }

        // Heuristics thresholds:
        const double MinRatioForCut = 1.5; // relative jump required
        double meanWeight = weights.Average();
        double MinAbsDiffForCut = meanWeight * 0.5; // absolute gap required

        if (maxRatio >= MinRatioForCut || maxDiff >= MinAbsDiffForCut)
        {
            // Choose the midpoint between the two weights that define the largest gap.
            // This ensures edges strictly larger than the gap are cut.
            return (weights[maxIdx - 1] + weights[maxIdx]) / 2.0;
        }

        // Fallback: conservative percentile (keeps smaller edges and prevents global linking)
        int idx = Math.Max(0, (int)Math.Floor(weights.Count * 0.75));
        return weights[idx];
    }
}
