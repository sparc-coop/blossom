using Microsoft.Extensions.Hosting;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Sparc.Blossom.Content;
using static Microsoft.ML.Transforms.LpNormNormalizingEstimatorBase;
using static Sparc.Blossom.Spaces.BlossomSpaceFaceter;

namespace Sparc.Blossom.Spaces;

public class ClusteringPrediction
{
    public string TargetUrl { get; set; } = "";
    public uint PredictedLabel { get; set; }
    public float[] Score { get; set; } = [];
}

public class BlossomSpaceFaceter(
    BlossomVectors vectors,
    IEnumerable<ITranslator> translators)
{
    public MLContext Context { get; } = new MLContext(seed: 1);
    BlossomVector? Root;
    List<BlossomVector>? PostVectors;
    List<BlossomVector>? Centroids;
    private PredictionEngine<BlossomVector, ClusteringPrediction>? Predictor;

    public async Task<List<BlossomSpace>> DivideAsync(BlossomSpace space, List<BlossomPost> posts, decimal sampleSize)
    {
        await vectors.ClearAsync(space.SpaceId, "Cluster");

        PostVectors = await vectors.GetAsync(space.SpaceId, "Post", sampleSize);
        Root = BlossomVector.Average(PostVectors);
        await vectors.UpdateAsync(Root);

        var model = Cluster(PostVectors);
        Predictor = Context.Model.CreatePredictionEngine<BlossomVector, ClusteringPrediction>(model, inputSchemaDefinition: BlossomVectorSchema());

        var spaces = await CreateSpaces(space, model);
        await AssignAsync(posts, Centroids!);

        foreach (var childSpace in spaces)
            await SummarizeAsync(childSpace, posts.Where(x => x.IsLinked(childSpace)));

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

    public async Task<List<BlossomSpace>> FacetAsync(BlossomSpace space, List<BlossomPost> posts, List<BlossomVector> postVectors)
    {
        // Factor into principal components
        posts = posts.Where(x => x.IsLinked(space)).ToList();
        var facets = BlossomVector.ToPrincipalComponents(postVectors, 0.8).Take(3);
        var facetSpaces = new List<BlossomSpace>();
        foreach (var facet in facets)
        {
            var facetSpace = new BlossomSpace(space, "Facet")
            {
                Id = facet.Id,
                Weight = facet.CoherenceWeight
            };
            facetSpaces.Add(facetSpace);
        }
        await vectors.UpdateAsync(facets);

        posts.ForEach(x => x.ClearLinks("Facet"));

        foreach (var facet in facets)
        {
            var axisPositions = postVectors.Where(x => posts.Any(y => y.Id == x.Id)).Select(x => x.PositionOnAxis(facet));
            var minPosition = axisPositions.Min();
            var maxPosition = axisPositions.Max();
            
            foreach (var post in posts)
            {
                var postVector = postVectors.FirstOrDefault(x => x.Id == post.Id);
                if (postVector != null)
                    post.LinkToSpace(facet.Id, "Facet", postVector.PositionOnAxis(facet, minPosition, maxPosition), postVector.Score(facet));
            }
        }

        foreach (var childFacetSpace in facetSpaces)
            await SummarizeAsync(childFacetSpace, posts);

        return facetSpaces;
    }

    public async Task AssignAsync(IEnumerable<BlossomPost> posts, List<BlossomVector> spaces)
    {
        if (Predictor == null || Root == null || PostVectors == null || Centroids == null)
            throw new InvalidOperationException("Model has not been trained. Please run RunAsync first.");

        var postVectors = new List<BlossomVector>();
        foreach (var post in posts)
        {
            var postVector = await vectors.FindAsync(post.SpaceId, post.Id);
            if (postVector == null)
            {
                Console.WriteLine($"Vector not found for post {post.Id}, skipping.");
                continue;
            }
            postVectors.Add(postVector);

            var prediction = Predictor.Predict(postVector);
            post.UnlinkAllSpaces();
            post.LinkToSpace(Root.SpaceId, "Space", postVector.DistanceTo(Root), postVector.AlignmentWith(Root));

            var predictedSpace = spaces[(int)prediction.PredictedLabel - 1];
            post.LinkToSpace(predictedSpace.Id, "Cluster", postVector.DistanceTo(predictedSpace), postVector.AlignmentWith(predictedSpace));
            Console.WriteLine($"Assigned post {post.Id} to space {predictedSpace.Id}.");
        }
    }

    public async Task SummarizeAsync(BlossomSpace space, IEnumerable<BlossomPost> assignedPosts)
    {
        var aiTranslator = translators.OfType<AITranslator>().First();
        if (space.RoomType == "Facet")
        {
            var leftVectors = await vectors.SearchAsync(space.ParentSpaceId!, space.Id, "Post", 5, true);
            leftVectors = leftVectors.Where(x => x.SimilarityToSpace < 0).ToList();

            var rightVectors = await vectors.SearchAsync(space.ParentSpaceId!, space.Id, "Post", 5);
            rightVectors = rightVectors.Where(x => x.SimilarityToSpace > 0).ToList();

            var leftPosts = assignedPosts
                .Where(x => leftVectors.Any(v => v.TargetUrl == x.Id))
                .ToList();

            var rightPosts = assignedPosts
                .Where(x => rightVectors.Any(v => v.TargetUrl == x.Id))
                .ToList();

            var summary = await aiTranslator.SummarizeAsync(leftPosts, rightPosts);
            space.SetSummary(summary);
        }
        else
        {
            var closestVectors = await vectors.SearchAsync(space.ParentSpaceId!, space.Id, "Post", 10);

            var matchingPosts = assignedPosts
                .Where(x => closestVectors.Any(v => v.TargetUrl == x.Id))
                .ToList();

            var summary = await aiTranslator.SummarizeAsync(matchingPosts);
            space.SetSummary(summary);
        }

        space.SetConsensus(assignedPosts);
    }

    private TransformerChain<ClusteringPredictionTransformer<KMeansModelParameters>> Cluster(List<BlossomVector> vectors)
    {
        var data = ToDataView(vectors);
        var kmeans = NormalizedKMeans(data, Math.Min(100, (int)Math.Sqrt(vectors.Count)));
        return kmeans.Fit(data);
    }

    private async Task<List<BlossomSpace>> CreateSpaces(BlossomSpace rootSpace, TransformerChain<ClusteringPredictionTransformer<KMeansModelParameters>> model)
    {
        var centroids = ExtractCentroids(model);
        var spaces = centroids.Select(x => new BlossomSpace(rootSpace, "Space")).ToList();
        Centroids = centroids.Select((x, i) => new BlossomVector(rootSpace.Id, "Space", spaces[i].Id, x)).ToList();
        await vectors.UpdateAsync(Centroids);
        return spaces;
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
