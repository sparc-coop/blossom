using DeepL;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Sparc.Blossom.Content;
using Sparc.Blossom.Data;
using Sparc.Blossom.Plugins.MLNet;
using static Microsoft.ML.Transforms.LpNormNormalizingEstimatorBase;

namespace Sparc.Blossom.Spaces;

public class BlossomVectors(
    IRepository<BlossomVector> vectors, 
    IRepository<BlossomPost> posts,
    IEnumerable<Content.ITranslator> translators,
    AzureBlobRepository files)
{
    public MLContext Context { get; } = new MLContext(seed: 1);

    public async Task<List<BlossomSpace>> Discover(BlossomSpace space, decimal sampleSize = 1M)
    {
        var (count, data) = await LoadAsync(space, sampleSize);
        var model = NormalizedKMeans(data, Math.Min(count, 100)).Fit(data);
        await SaveModelAsync(model, space, data.Schema);
        var spaces = await ExtractSpaces(space, model);
        await AssignDataToSpaces(model, spaces, data);
        return spaces;
    }

    public async Task<List<BlossomPost>> GetRelevantPostsAsync(BlossomSpace space, int count, bool fuzzy = false)
    {
        if (!fuzzy)
        {
            var exactPosts = await posts.Query
                .Where(x => x.SpaceId == space.Id || x.MostRelevantSpaceId == space.Id)
                .Take(count)
                .ToListAsync();
            return exactPosts;
        }
        
        var spaceVector = await vectors.Query
            .Where(x => x.SpaceId == space.Id && x.TargetUrl == space.Id)
            .Select(x => x.Vector)
            .FirstOrDefaultAsync()
            ?? throw new Exception("Space vector not found");

        var query = $@"
            SELECT TOP {count} VALUE c.TargetUrl
            FROM c
            WHERE c.SpaceId = '{space.ParentSpaceId}' AND c.TargetUrl != '{space.ParentSpaceId}'
            ORDER BY VectorDistance(c.Vector, {new BlossomVector(spaceVector)})";

        var cosmosVectors = vectors as CosmosDbSimpleRepository<BlossomVector>;
        var similarVectorsInSpace = await cosmosVectors!.FromSqlAsync<string>(query, space.ParentSpaceId);

        var postsInSpace = await posts.Query
            .Where(x => similarVectorsInSpace.Contains(x.PostId))
            .ToListAsync();

        return postsInSpace;
    }

    private EstimatorChain<ClusteringPredictionTransformer<KMeansModelParameters>> NormalizedKMeans(IDataView data, int maxSpaces = 100)
    {
        var scores = new List<double>();

        for (var numSpaces = 1; numSpaces <= maxSpaces; numSpaces++)
        {
            var model = CreateModel(numSpaces);
            var predictions = model.Fit(data).Transform(data);
            var metrics = Context.Clustering.Evaluate(predictions);
            scores.Add(metrics.AverageDistance);
        }

        var elbowK = FindElbow(scores, 1);
        return CreateModel(elbowK);
    }

    private EstimatorChain<ClusteringPredictionTransformer<KMeansModelParameters>> CreateModel(int numSpaces)
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

    private async Task<(int, IDataView)> LoadAsync(BlossomSpace space, decimal sampleSize)
    {
        var query = vectors.Query.Where(x => x.SpaceId == space.Id);
        int take = (int)(query.Count() * sampleSize);

        var spaceVectors = await query
            .OrderBy(x => x.Id)
            .Take(take)
            .ToListAsync();

        var dataView = await LoadAsync(spaceVectors);
        return (spaceVectors.Count, dataView);
    }

    private async Task<IDataView> LoadAsync(IEnumerable<BlossomVector> vectors)
    { 
        return Context.Data.LoadFromEnumerable(FixedVector.From(vectors));
    }

    private async Task SaveModelAsync(ITransformer model, BlossomSpace space, DataViewSchema schema)
    {
        using var stream = new MemoryStream();
        Context.Model.Save(model, schema, stream);

        var file = new BlossomFile("models", $"{space.SpaceId}/{Guid.NewGuid()}.zip", AccessTypes.Private, stream);
        await files.AddAsync(file);
        space.ModelUrl = file.Url;
    } 

    private async Task<List<BlossomSpace>> ExtractSpaces(BlossomSpace rootSpace, TransformerChain<ClusteringPredictionTransformer<KMeansModelParameters>> model)
    {
        VBuffer<float>[] centroids = [];
        model!.LastTransformer.Model.GetClusterCentroids(ref centroids, out int k);

        var newSpaces = new List<BlossomSpace>();
        for (int i = 0; i < k; i++)
        {
            var space = rootSpace.CreateChild();
            newSpaces.Add(space);

            var vector = new BlossomVector(space.SpaceId, "text-embedding-3-small", [.. centroids[i].DenseValues()], space.SpaceId);
            await vectors.AddAsync(vector);
        }

        return newSpaces;
    }

    private async Task AssignDataToSpaces(TransformerChain<ClusteringPredictionTransformer<KMeansModelParameters>> model, List<BlossomSpace> spaces, IDataView data)
    {
        var parentSpaceId = spaces.First().ParentSpaceId;
        var query = await vectors.Query.Where(x => x.SpaceId == parentSpaceId).ToListAsync();
        var fixedVectors = FixedVector.From(query);
        var predictor = Context.Model.CreatePredictionEngine<FixedVector, ClusteringPrediction>(model);

        foreach (var vector in fixedVectors)
        {
            var prediction = predictor.Predict(vector);
            var post = await posts.FindAsync(vector.TargetUrl);
            if (post != null)
            {
                post.MostRelevantSpaceId = spaces[(int)prediction.PredictedLabel - 1].Id;
                await posts.UpdateAsync(post);
                Console.WriteLine($"Assigned post {post.Id} to space {post.MostRelevantSpaceId}.");
            }
        }
    }


    public async Task Transform(ITransformer transformer, IEnumerable<BlossomVector> vectors)
    {
        var data = Context.Data.LoadFromEnumerable(FixedVector.From(vectors));
        var predictions = transformer.Transform(data);
        var clusteredResults = Context.Data.CreateEnumerable<ClusteringPrediction>(predictions, reuseRowObject: false).ToList();
        // Output the clustering results
        for (int i = 0; i < vectors.Count(); i++)
        {
            var vector = vectors.ElementAt(i);
            var prediction = clusteredResults[i];
            Console.WriteLine($"Vector TargetUrl: {prediction.TargetUrl}, Assigned Cluster: {prediction.PredictedLabel}, Score: {prediction.Score}");
        }
    }

    public static int FindElbow(List<double> inertias, int minK = 1)
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
        List<double> diffs = new List<double>();
        for (int i = 0; i < n; i++)
        {
            diffs.Add(line[i] - norm[i]);
        }

        // Find index of maximum deviation
        int elbowIndex = diffs.IndexOf(diffs.Max());

        // Adjust for minK offset
        return minK + elbowIndex;
    }

    internal async Task AddAsync(BlossomPost post)
    {
        var translator = translators.OfType<OpenAITranslator>().First();
        var vector = await translator.VectorizeAsync(post);
        await vectors.AddAsync(vector);
    }
}

