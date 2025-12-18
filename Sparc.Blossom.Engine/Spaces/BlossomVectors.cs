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
    AzureBlobRepository files)
{
    public MLContext Context { get; } = new MLContext(seed: 1);

    public async Task<List<BlossomSpace>> Discover(BlossomSpace space, int numSpaces, decimal sampleSize = 1M)
    {
        var data = await LoadAsync(space, sampleSize);
        var model = NormalizedKMeans(numSpaces).Fit(data);
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
                .Where(x => x.MostRelevantSpaceId == space.Id)
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

    private EstimatorChain<ClusteringPredictionTransformer<KMeansModelParameters>> NormalizedKMeans(int numSpaces)
    {
        var normalize = Context.Transforms.NormalizeLpNorm("Vector", norm: NormFunction.L2);
        var kmeans = Context.Clustering.Trainers.KMeans(featureColumnName: "Vector", numberOfClusters: numSpaces);
        var pipeline = normalize.Append(kmeans);
        return pipeline;
    }

    private async Task<IDataView> LoadAsync(BlossomSpace space, decimal sampleSize)
    {
        var query = vectors.Query.Where(x => x.SpaceId == space.Id);
        int take = (int)(query.Count() * sampleSize);

        var spaceVectors = await query
            .OrderBy(x => x.Id)
            .Take(take)
            .ToListAsync();

        return await LoadAsync(spaceVectors);
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
}

