using Sparc.Blossom.Content;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Spaces;

internal class BlossomSpaceConstellator(
    IRepository<Constellation> constellations,
    BlossomPosts postRepository,
    IEnumerable<ITranslator> translators)
{
    public async Task<List<Constellation>> ConstellateAsync(BlossomSpace space, List<Axis> axes)
    {
        var posts = await postRepository.GetAllAsync(space, 10000);

        if (posts.Count() < 3)
            return [];

        posts.ForEach(p => p.MaterializeCoordinates(axes));
        var edges = ComputeKnnEdges(posts, space.Settings.ConstellationStrength);
        var result = Kruskal(posts, edges, space.Settings.ConstellationThreshold);
        var newConstellations = await CreateConstellations(space, result);

        await postRepository.UpdateAsync(posts);

        var translator = translators.OfType<AITranslator>().First();
        await Parallel.ForEachAsync(newConstellations, async (constellation, _) =>
        {
            var postsInConstellation = posts.Where(p => p.ConstellationId == constellation.Id).ToList();
            constellation.SetSummary(await translator.SummarizeAsync(postsInConstellation));
        });

        return newConstellations;
    }

    private async Task<List<Constellation>> CreateConstellations(BlossomSpace space, Dictionary<Post, List<Post>> vectors)
    {
        // Create constellation vectors as simple average of coordinates per component
        var existing = await constellations.Query.Where(x => x.SpaceId == space.Id).ToListAsync();
        await constellations.DeleteAsync(existing);

        foreach (var posts in vectors.Values)
            posts.ForEach(x => x.SetConstellation(null, null));

        var result = new List<Constellation>();
        foreach (var root in vectors.Keys)
        {
            var constellationVectors = vectors[root].Select(x => x.Vector);
            var centroid = BlossomVector.Average(constellationVectors);
            var constellation = new Constellation(space, centroid);
            result.Add(constellation);

            for (var i = 0; i < vectors[root].Count; i++)
            {
                var post = vectors[root][i];
                var prev = i > 0 ? vectors[root][i - 1] : null;
                post.SetConstellation(constellation, prev);
            }
        }

        await constellations.UpdateAsync(result);
        return result;
    }

    record KnnEdge(Post From, Post To, double Distance);
    static List<KnnEdge> ComputeKnnEdges(List<Post> posts, int k)
    {
        var edges = new List<KnnEdge>();
        var count = posts.Count;
        
        for (int i = 0; i < count; i++)
        {
            var distances = new List<(int idx, double distance)>();
            for (int j = 0; j < count; j++)
            {
                if (i == j) continue;
                var distance = posts[i].Coordinates!.DistanceTo(posts[j].Coordinates!);
                distances.Add((j, distance));
            }

            k = Math.Max(1, Math.Min(k, posts.Count - 1));
            foreach (var (idx, distance) in distances.OrderBy(x => x.distance).Take(k))
            {
                var post1 = posts[Math.Min(i, idx)];
                var post2 = posts[Math.Max(i, idx)];
                if (!edges.Any(e => e.From == post1 && e.To == post2))
                    edges.Add(new(post1!, post2!, distance));
            }
        }

        return edges.OrderBy(e => e.Distance).ToList();
    }

    static Dictionary<Post, List<Post>> Kruskal(List<Post> posts, List<KnnEdge> edges, double threshold)
    {
        var n = posts.Count;
        var parent = new int[n];
        for (int i = 0; i < n; i++) parent[i] = i;

        int Find(int x) => parent[x] == x ? x : (parent[x] = Find(parent[x]));
        void Union(int x, int y) { parent[Find(y)] = Find(x); }

        // Build MST using Kruskal over provided candidate edges
        var mstEdges = new List<(int a, int b, double dist)>();
        foreach (var e in edges.OrderBy(e => e.Distance))
        {
            int a = posts.IndexOf(e.From);
            int b = posts.IndexOf(e.To);
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
        var constellations = new Dictionary<Post, List<Post>>();
        for (int i = 0; i < n; i++)
        {
            int r = Find(i);
            if (!constellations.ContainsKey(posts[r])) constellations[posts[r]] = [];
            constellations[posts[r]].Add(posts[i]);
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
