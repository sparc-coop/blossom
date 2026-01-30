using Sparc.Blossom.Content;

namespace Sparc.Blossom.Spaces;

public class BlossomSpaceConstellator(BlossomVectors vectors)
{
    public async Task<List<BlossomVector>> ConstellateAsync(BlossomSpace rootSpace, List<BlossomPostWithVector> posts, List<BlossomVector> axes, int k = 5)
    {
        if (posts.Count < 3)
            return [];
        
        var coords = posts.Select(p => p.Vector.ProjectOntoAxes(axes)).ToList();
        int n = coords.Count;
        k = Math.Max(1, Math.Min(k, n - 1));

        // Build k-NN sparse undirected edge list
        var edges = new List<(int a, int b, double w)>();
        for (int i = 0; i < n; i++)
        {
            var dists = new List<(int idx, double d)>();
            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;
                var d = coords[i].DistanceTo(coords[j]) ?? double.MaxValue;
                dists.Add((j, d));
            }

            foreach (var nb in dists.OrderBy(x => x.d).Take(k))
            {
                int a = Math.Min(i, nb.idx);
                int b = Math.Max(i, nb.idx);
                if (!edges.Any(e => e.a == a && e.b == b))
                    edges.Add((a, b, nb.d));
            }
        }

        // Kruskal's algorithm to build MST over the sparse graph
        var sorted = edges.OrderBy(e => e.w).ToList();
        var parent = new int[n];
        for (int i = 0; i < n; i++) parent[i] = i;
        int Find(int x) => parent[x] == x ? x : (parent[x] = Find(parent[x]));
        bool Union(int x, int y)
        {
            int rx = Find(x), ry = Find(y);
            if (rx == ry) return false;
            parent[ry] = rx;
            return true;
        }

        var mst = new List<(int a, int b, double w)>();
        foreach (var e in sorted)
        {
            if (Union(e.a, e.b))
                mst.Add(e);
            if (mst.Count == n - 1) break;
        }

        // If MST is empty (disconnected), treat everything as one cluster
        if (mst.Count == 0)
            return [];

        // Determine cut threshold by finding the largest gap in MST edge weights
        var weights = mst.Select(e => e.w).OrderBy(w => w).ToList();
        double threshold = double.MaxValue;
        if (weights.Count > 1)
        {
            double maxDiff = 0; int maxIdx = 0;
            for (int i = 1; i < weights.Count; i++)
            {
                var diff = weights[i] - weights[i - 1];
                if (diff > maxDiff) { maxDiff = diff; maxIdx = i; }
            }
            if (maxDiff > 0)
                threshold = weights[maxIdx - 1];
        }

        // Rebuild unions only for MST edges that are <= threshold (keeps small edges)
        for (int i = 0; i < n; i++) parent[i] = i;
        foreach (var e in mst)
        {
            if (e.w <= threshold) parent[Find(e.b)] = Find(e.a);
        }

        // Collect components
        var comps = new Dictionary<int, List<int>>();
        for (int i = 0; i < n; i++)
        {
            int r = Find(i);
            if (!comps.ContainsKey(r)) comps[r] = new List<int>();
            comps[r].Add(i);
        }

        // Create constellation vectors as simple average of coordinates per component
        await vectors.ClearAsync(rootSpace.Id, "Constellation");
        var result = new List<BlossomVector>();
        foreach (var comp in comps.Values)
        {
            var c = comp.Select(idx => coords[idx]).ToList();
            var centroid = BlossomVector.Average(c);
            var bv = new BlossomVector(rootSpace.Id, "Constellation", Guid.NewGuid().ToString(), centroid.Vector);
            result.Add(bv);
        }

        await vectors.UpdateAsync(result);

        // Assign posts to constellations
        int ci = 0;
        foreach (var comp in comps.Values)
        {
            var constellationId = result[ci].Id;
            foreach (var idx in comp)
                posts[idx].Post.ConstellationId = constellationId;
            ci++;
        }

        foreach (var vec in result)
            await vectors.SummarizeAsync(vec);

        return result;
    }
}
