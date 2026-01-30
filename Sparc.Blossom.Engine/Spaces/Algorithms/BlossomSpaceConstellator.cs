using Sparc.Blossom.Content;

namespace Sparc.Blossom.Spaces;

public class BlossomSpaceConstellator(BlossomVectors vectors, IRepository<BlossomPost> postRepository)
{
    public async Task<List<BlossomVector>> ConstellateAsync(BlossomSpace rootSpace, List<BlossomPostWithVector> posts, List<BlossomVector> axes, int k = 5)
    {
        if (posts.Count < 3)
            return [];

        var coords = posts.Select(p => p.Vector.ProjectOntoAxes(axes)).ToList();
        k = Math.Max(1, Math.Min(k, coords.Count - 1));

        var edges = ComputeKnnEdges(coords, k);
        var constellations = Kruskal(coords, edges);
        var constellationVectors = await CreateConstellations(rootSpace, posts, constellations);

        await vectors.UpdateAsync(posts.Select(p => p.Vector));
        await postRepository.UpdateAsync(posts.Select(p => p.Post));

        foreach (var vec in constellationVectors)
            await vectors.SummarizeAsync(vec);

        return constellationVectors;
    }

    private async Task<List<BlossomVector>> CreateConstellations(BlossomSpace rootSpace, List<BlossomPostWithVector> posts, Dictionary<BlossomVector, List<BlossomVector>> constellations)
    {
        // Create constellation vectors as simple average of coordinates per component
        await vectors.ClearAsync(rootSpace.Id, "Constellation");
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

    static Dictionary<BlossomVector, List<BlossomVector>> Kruskal(List<BlossomVector> coords, List<KnnEdge> edges)
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
        double threshold = FindMstCutThreshold(mstEdges);

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

    static double FindMstCutThreshold(List<(int a, int b, double dist)> mstEdges)
    {
        var weights = mstEdges.Select(e => e.dist).OrderBy(d => d).ToList();
        double threshold = double.MaxValue;
        double maxDiff = 0; int maxIdx = 0;
        for (int i = 1; i < weights.Count; i++)
        {
            var diff = weights[i] - weights[i - 1];
            if (diff > maxDiff) { maxDiff = diff; maxIdx = i; }
        }
        if (maxDiff > 0)
            threshold = weights[maxIdx - 1];
        return threshold;
    }
}
