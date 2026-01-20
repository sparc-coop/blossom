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

    public async Task<List<BlossomSpaceWithVector>> DivideAsync(BlossomSpace space, List<BlossomPostWithVector> posts)
    {
        await vectors.ClearAsync(space.SpaceId, "Cluster");
        var root = BlossomVector.Average(posts.Select(x => x.Vector));
        await vectors.UpdateAsync(root);

        var model = Cluster(posts.Select(x => x.Vector).ToList());
        Predictor = Context.Model.CreatePredictionEngine<BlossomVector, ClusteringPrediction>(model, inputSchemaDefinition: BlossomVectorSchema());
        var spaces = await CreateSpaces(space, model);
        await AssignAsync(posts, spaces);

        //foreach (var childSpace in spaces)
        //    await SummarizeAsync(childSpace.Space, posts.Where(x => x.Post.IsLinked(childSpace.Space)).Select(x => x.Post));

        return spaces;
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

    public async Task<List<BlossomSpaceWithVector>> FacetAsync(BlossomSpaceWithVector space, List<BlossomPostWithVector> posts)
    {
        if (posts.Count < 2)
            return [];
        
        // Factor into principal components
        posts = posts.Where(x => x.Post.IsLinked(space.Space)).ToList();
        var facets = BlossomVector.ToPrincipalComponents(posts.Select(x => x.Vector), 0.8).Take(3);
        var facetSpaces = new List<BlossomSpaceWithVector>();
        foreach (var facet in facets)
        {
            var facetSpace = new BlossomSpaceWithVector(new(space.Space, "Facet")
            {
                Id = facet.Id,
                Weight = facet.CoherenceWeight
            }, facet);
            facetSpace.LinkToSpace(space);
            facetSpaces.Add(facetSpace);
        }

        //await Parallel.ForEachAsync(facetSpaces, async (childFacetSpace, _) => 
        //    await SummarizeAsync(childFacetSpace, posts.Select(x => x.Post)));

        return facetSpaces;
    }

    public async Task AssignAsync(IEnumerable<BlossomPostWithVector> posts, List<BlossomSpaceWithVector> spaces)
    {
        if (Predictor == null)
            throw new InvalidOperationException("Model has not been trained. Please run RunAsync first.");

        foreach (var post in posts.Where(x => x.Vector != null))
        {
            var prediction = Predictor.Predict(post.Vector);
            var predictedSpace = spaces[(int)prediction.PredictedLabel - 1];
            post.LinkToSpace(predictedSpace, predictedSpace);
        }
    }

    

    private TransformerChain<ClusteringPredictionTransformer<KMeansModelParameters>> Cluster(List<BlossomVector> vectors)
    {
        var data = ToDataView(vectors);
        var kmeans = NormalizedKMeans(data, Math.Min(100, (int)Math.Sqrt(vectors.Count)));
        return kmeans.Fit(data);
    }

    private async Task<List<BlossomSpaceWithVector>> CreateSpaces(BlossomSpace rootSpace, TransformerChain<ClusteringPredictionTransformer<KMeansModelParameters>> model)
    {
        var centroids = ExtractCentroids(model);
        var spaces = centroids.Select(x => new BlossomSpace(rootSpace, "Space")).ToList();
        var result = centroids.Select((x, i) => new BlossomSpaceWithVector(spaces[i], x)).ToList();
        await vectors.UpdateAsync(result.Select(x => x.Vector));
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
        var schema = SchemaDefinition.Create(typeof(BlossomVector), SchemaDefinition.Direction.Write);
        schema[nameof(BlossomVector.Vector)].ColumnType = new VectorDataViewType(NumberDataViewType.Single, 1536);
        return schema;
    }


    private IDataView ToDataView(List<BlossomVector> vectors) => Context.Data.LoadFromEnumerable(vectors, BlossomVectorSchema());
}
