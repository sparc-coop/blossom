using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Sparc.Blossom.Content;
using static Microsoft.ML.Transforms.LpNormNormalizingEstimatorBase;

namespace Sparc.Blossom.Spaces;

public class ClusteringPrediction
{
    public string TargetUrl { get; set; } = "";
    public uint PredictedLabel { get; set; }
    public float[] Score { get; set; } = [];
}

public class BlossomSpaceFaceter(BlossomVectors vectors)
{
    public MLContext Context { get; } = new MLContext(seed: 1);
    private PredictionEngine<BlossomVector, ClusteringPrediction>? Predictor;

    public async Task<List<BlossomVector>> ClusterAsync(BlossomSpace space, List<BlossomPostWithVector> posts, List<BlossomVector> axes)
    {
        //var root = BlossomVector.Average(posts.Select(x => x.Vector));
        //await vectors.UpdateAsync(root);

        var coordinates = posts.Select(x => x.Vector.ProjectOntoAxes(axes)).ToList();
        var model = Cluster(coordinates);
        Predictor = Context.Model.CreatePredictionEngine<BlossomVector, ClusteringPrediction>(model, inputSchemaDefinition: BlossomVectorSchema());
        var clusterVectors = await CreateClusterVectors(space, model);
        await AssignAsync(posts, clusterVectors, axes);

        foreach (var vec in clusterVectors)
            await vectors.SummarizeAsync(vec);

        return clusterVectors;
    }

    public record PostVector(BlossomPost Post, BlossomVector Vector);
    //public async Task<BlossomVector> AnswerAsync(BlossomSpace space, List<BlossomPost> posts)
    //{
    //    PostVectors = await vectors.GetAsync(space.SpaceId, "Post", 1M);

    //    var postsWithVectors = posts.OrderBy(x => x.Timestamp)
    //        .Select(x => new PostVector(x, PostVectors.FirstOrDefault(y => y.Id == x.Id)))
    //        .Where(x => x.Vector != null)
    //        .ToList();
        
    //    // Set the first post's answer to its own vector
    //    var firstPost = postsWithVectors.First();
    //    firstPost.Vector.Point = firstPost.Vector.Vector;

    //    for (var i = 1; i < postsWithVectors.Count; i++)
    //    {
    //        var item = postsWithVectors[i];
    //        var prev = postsWithVectors[i - 1];
    //        if (i == 1)
    //            item.Vector.Point = BlossomVector.Average([prev.Vector, item.Vector]).Vector;
    //        else
    //            item.Vector.SetAnswer(postsWithVectors.Take(i).Select(x => x.Vector));

    //        item.Post.Information = item.Vector.Information;
    //        prev.Post.Maturity = prev.Vector.Maturity;
    //    }

    //    Root = new BlossomVector(space.Id, "Space", postsWithVectors.Last().Vector.Point!);
    //    await vectors.UpdateAsync(Root);

    //    foreach (var post in postsWithVectors)
    //        post.Post.LinkToSpace(space.Id, post.Vector.DistanceTo(new(Root.Vector)), post.Vector.AlignmentWith(new(Root.Vector)));

    //    await vectors.UpdateAsync(postsWithVectors.Last().Vector);
    //    return Root;
    //}

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
                .OrderByDescending(x => x.AlignmentWith(facet) ?? 0)
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

    public async Task AssignAsync(IEnumerable<BlossomPostWithVector> posts, List<BlossomVector> clusterVectors, List<BlossomVector> axes)
    {
        if (Predictor == null)
            throw new InvalidOperationException("Model has not been trained. Please run RunAsync first.");

        foreach (var post in posts.Where(x => x.Vector != null))
        {
            var prediction = Predictor.Predict(post.Vector.ProjectOntoAxes(axes));
            var predictedCluster = clusterVectors[(int)prediction.PredictedLabel - 1];
            post.Post.ConstellationId = predictedCluster.Id;
        }
    }

    private TransformerChain<ClusteringPredictionTransformer<KMeansModelParameters>> Cluster(List<BlossomVector> vectors)
    {
        var data = ToDataView(vectors);
        var kmeans = NormalizedKMeans(data, Math.Min(100, (int)Math.Sqrt(vectors.Count)));
        return kmeans.Fit(data);
    }

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

    private async Task<List<BlossomVector>> CreateClusterVectors(BlossomSpace rootSpace, TransformerChain<ClusteringPredictionTransformer<KMeansModelParameters>> model)
    {
        await vectors.ClearAsync(rootSpace.Id, "Constellation");
        var centroids = ExtractCentroids(model);
        var result = centroids.Select(x => new BlossomVector(rootSpace.Id, "Constellation", Guid.NewGuid().ToString(), x)).ToList();
        await vectors.UpdateAsync(result);
        return result;
    }

    private EstimatorChain<ClusteringPredictionTransformer<KMeansModelParameters>> NormalizedKMeans(IDataView data, int maxSpaces = 100)
    {
        var scores = new List<double>();

        for (var numSpaces = 1; numSpaces <= maxSpaces; numSpaces++)
        {
            try
            {
                var predictions = NormalizedKMeansModel(numSpaces).Fit(data).Transform(data);
                var metrics = Context.Clustering.Evaluate(predictions);
                scores.Add(metrics.AverageDistance);
            }
            catch
            {
                break;
            }
        }

        var elbowK = FindElbow(scores, 1);
        return NormalizedKMeansModel(elbowK);
    }

    private EstimatorChain<ClusteringPredictionTransformer<KMeansModelParameters>> NormalizedKMeansModel(int numSpaces)
    {
        var normalize = Context.Transforms.NormalizeLpNorm("Vector", norm: NormFunction.L2);
        var options = new KMeansTrainer.Options
        {
            InitializationAlgorithm = KMeansTrainer.InitializationAlgorithm.KMeansPlusPlus,
            FeatureColumnName = "Vector",
            NumberOfClusters = numSpaces
        };
        var kmeans = Context.Clustering.Trainers.KMeans(options);
        var pipeline = normalize.Append(kmeans);
        return pipeline;
    }

    private static List<float[]> ExtractCentroids(TransformerChain<ClusteringPredictionTransformer<KMeansModelParameters>> model)
    {
        VBuffer<float>[] centroids = [];
        model!.LastTransformer.Model.GetClusterCentroids(ref centroids, out int k);

        List<float[]> newVectors = [];
        for (int i = 0; i < k; i++)
            newVectors.Add([.. centroids[i].DenseValues()]);

        return newVectors;
    }

    private static int FindElbow(List<double> inertias, int minK = 1)
    {
        int n = inertias.Count;
        // Normalize inertia values between 0 and 1
        double maxVal = inertias.Max();
        double minVal = inertias.Min();
        List<double> norm = inertias.Select(v => (v - minVal) / (maxVal - minVal)).ToList();

        // Create straight line from first to last point
        List<double> line = [];
        for (int i = 0; i < n; i++)
        {
            double frac = (double)i / (n - 1);
            line.Add(1 - frac); // decreasing line from 1 to 0
        }

        // Compute difference between curve and line
        List<double> diffs = [];
        for (int i = 0; i < n; i++)
        {
            diffs.Add(line[i] - norm[i]);
        }

        // Find index of maximum deviation
        int elbowIndex = diffs.IndexOf(diffs.Max());

        // Adjust for minK offset
        return minK + elbowIndex;
    }

    private async Task SaveModelAsync(IRepository<BlossomFile> files, ITransformer model, BlossomSpace space, DataViewSchema schema)
    {
        using var stream = new MemoryStream();
        Context.Model.Save(model, schema, stream);

        var file = new BlossomFile("models", $"{space.SpaceId}/{Guid.NewGuid()}.zip", AccessTypes.Private, stream);
        await files.AddAsync(file);
        space.ModelUrl = file.Url;
    }

    private static SchemaDefinition BlossomVectorSchema()
    {
        var schema = SchemaDefinition.Create(typeof(BlossomVectorBase), SchemaDefinition.Direction.Write);
        schema[nameof(BlossomVector.Vector)].ColumnType = new VectorDataViewType(NumberDataViewType.Single, 2);
        return schema;
    }


    private IDataView ToDataView(List<BlossomVector> vectors) => Context.Data.LoadFromEnumerable(vectors, BlossomVectorSchema());
}
