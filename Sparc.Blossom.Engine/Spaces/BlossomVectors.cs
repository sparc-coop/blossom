using MathNet.Numerics.LinearAlgebra;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Sparc.Blossom.Content;
using Sparc.Blossom.Data;
using static Microsoft.ML.Transforms.LpNormNormalizingEstimatorBase;

namespace Sparc.Blossom.Spaces;

public class BlossomVectors(
    IRepository<BlossomVector> vectors, 
    IRepository<BlossomPost> posts,
    IEnumerable<ITranslator> translators,
    AzureBlobRepository files)
{
    public MLContext Context { get; } = new MLContext(seed: 1);

    public async Task<List<BlossomSpace>> Discover(BlossomSpace rootSpace, decimal samplePercentage = 1M)
    {
        var (rootCentroid, vectors) = await LoadAsync(rootSpace, samplePercentage);
        var model = Cluster(vectors);
        //await SaveModelAsync(model, space, data.Schema);

        var spaces = await CreateSpaces(rootSpace, rootCentroid, model);
        return spaces;
    }

    public TransformerChain<ClusteringPredictionTransformer<KMeansModelParameters>> Cluster(List<BlossomVector> vectors)
    {
        var data = ToDataView(vectors);
        var kmeans = NormalizedKMeans(data, Math.Min(100, (int)Math.Sqrt(vectors.Count)));
        return kmeans.Fit(data);
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

    private async Task<(BlossomVector root, List<BlossomVector> children)> LoadAsync(BlossomSpace space, decimal sampleSize)
    {
        var query = vectors.Query.Where(x => x.SpaceId == space.Id);
        int take = (int)(query.Count() * sampleSize);

        var spaceVectors = await query
            .OrderBy(x => x.Id)
            .Take(take)
            .ToListAsync();

        var rootCentroid = CalculateRootVector(spaceVectors);
        return (rootCentroid, spaceVectors);
    }

    private static BlossomVector CalculateRootVector(List<BlossomVector> spaceVectors)
    {
        var mean = BlossomVector.Average(spaceVectors);
        var meanVector = Vector<float>.Build.Dense(mean.Vector);
        Matrix<float> matrix = ToMatrix(spaceVectors);

        // Subtract each vector by the mean
        for (int i = 0; i < matrix.RowCount; i++)
            matrix.SetRow(i, matrix.Row(i) - meanVector);

        var svd = matrix.Svd(computeVectors: true);
        var firstPrincipal = svd.VT.Row(0);
        var rootCentroid = new BlossomVector(spaceVectors.First().SpaceId, [.. firstPrincipal])
        {
            Point = mean.Vector
        };
        Console.WriteLine($"Calculated root centroid with length {rootCentroid.Magnitude()}");
        return rootCentroid;
    }

    private static Matrix<float> ToMatrix(List<BlossomVector> spaceVectors)
    {
        return Matrix<float>.Build.Dense(spaceVectors.Count, 1536, (i, j) => spaceVectors[i].Vector[j]);
    }

    private static SchemaDefinition BlossomVectorSchema()
    {
        var schema = SchemaDefinition.Create(typeof(BlossomVector), SchemaDefinition.Direction.Write);
        schema[nameof(BlossomVector.Vector)].ColumnType = new VectorDataViewType(NumberDataViewType.Single, 1536);
        return schema;
    }

    private IDataView ToDataView(List<BlossomVector> vectors) => Context.Data.LoadFromEnumerable(vectors, BlossomVectorSchema());

    private async Task SaveModelAsync(ITransformer model, BlossomSpace space, DataViewSchema schema)
    {
        using var stream = new MemoryStream();
        Context.Model.Save(model, schema, stream);

        var file = new BlossomFile("models", $"{space.SpaceId}/{Guid.NewGuid()}.zip", AccessTypes.Private, stream);
        await files.AddAsync(file);
        space.ModelUrl = file.Url;
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

    private async Task<List<BlossomSpace>> CreateSpaces(BlossomSpace rootSpace, BlossomVector rootCentroid, TransformerChain<ClusteringPredictionTransformer<KMeansModelParameters>> model)
    {
        var centroids = ExtractCentroids(model);
        var spaces = centroids.Select(x => new BlossomSpace(rootSpace)).ToList();
        var spaceVectors = centroids.Select((x, i) => new BlossomVector(spaces[i].Id, [])
        {
            Point = x
        }).ToList();

        var predictor = Context.Model.CreatePredictionEngine<BlossomVector, ClusteringPrediction>(model, inputSchemaDefinition: BlossomVectorSchema());
        var postsInSpace = await posts.Query.Where(x => x.SpaceId == rootSpace.Id).ToListAsync();
        var postVectors = new List<BlossomVector>();
        foreach (var post in postsInSpace)
        {
            var postVector = await vectors.FindAsync(post.SpaceId, post.Id);
            if (postVector == null)
            {
                Console.WriteLine($"Vector not found for post {post.Id}, skipping.");
                continue;
            }
            postVectors.Add(postVector);

            var prediction = predictor.Predict(postVector);
            post.UnlinkAllSpaces();
            post.LinkToSpace(postVector, rootCentroid);
            
            var predictedSpace = spaceVectors[(int)prediction.PredictedLabel - 1];
            post.LinkToSpace(postVector, predictedSpace);
            Console.WriteLine($"Assigned post {post.Id} to space {predictedSpace.SpaceId}.");
        }

        // Calculate PCA on each subspace
        foreach (var spaceVector in spaceVectors)
        {
            var posts = postsInSpace.Where(x => x.LinkedSpace(spaceVector.SpaceId) != null).ToList();
            var result = CalculateRootVector(posts.SelectMany(x => postVectors.Where(y => y.Id == x.Id)).ToList());
            spaceVector.Vector = result.Vector;

            foreach (var post in posts)
                post.LinkToSpace(postVectors.First(x => x.Id == post.Id), spaceVector);
        }

        await posts.UpdateAsync(postsInSpace);
        //await vectors.UpdateAsync(postVectors);
        await vectors.UpdateAsync(spaceVectors);
        return spaces;
    }

    public async Task<List<string>> SearchAsync(BlossomSpace space, int count)
    {
        var spaceVector = await vectors.Query
            .Where(x => x.SpaceId == space.Id && x.Id == space.Id)
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
        return similarVectorsInSpace;
    }

    internal async Task IndexAsync(string spaceId)
    {
        var existing = await vectors.Query.Where(x => x.SpaceId == spaceId).ToListAsync();
        if (existing.Count != 0)
            await vectors.DeleteAsync(existing);

        var messages = await posts.Query.Where(x => x.Domain == BlossomSpaces.Domain && x.SpaceId == spaceId).ToListAsync();
        var offset = 0;
        var batchSize = 1000;

        do
        {
            var batch = messages
                        .Where(x => !string.IsNullOrWhiteSpace(x.Text))
                        //.OrderBy(x => x.Sequence)
                        .Skip(offset)
                        .Take(batchSize)
                        .ToList();

            var ids = batch.Select(x => x.Id).ToList();
            //var existing = await vectorRepo.Query.Where(x => ids.Contains(x.TargetUrl)).Select(x => x.TargetUrl).ToListAsync();
            //batch = batch.Where(x => !existing.Contains(x.Id)).ToList();
            if (batch.Count > 0)
            {
                var translator = translators.OfType<OpenAITranslator>().First();
                var newVectors = await translator.VectorizeAsync(batch);
                await vectors.AddAsync(newVectors);
            }
            offset += batchSize;
        } while (offset < messages.Count);
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

