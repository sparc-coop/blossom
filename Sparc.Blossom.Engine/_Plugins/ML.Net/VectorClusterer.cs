using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Sparc.Blossom.Content;
using static Microsoft.ML.Transforms.LpNormNormalizingEstimatorBase;

namespace Sparc.Blossom.Plugins.MLNet;

public class FixedVector(string targetUrl, float[] vector)
{
    public string TargetUrl { get; set; } = targetUrl;
    [VectorType(1536)]
    public float[] Vector { get; set; } = vector;
}

public class VectorClusterer
{
    public MLContext Context { get; }

    public VectorClusterer()
    {
        Context = new MLContext(seed: 1);
    }

    public IDataView Load(IEnumerable<BlossomVector> vectors)
    {
        return Context.Data.LoadFromEnumerable(vectors.Select(x => new FixedVector(x.TargetUrl, x.Vector)));
    }

    public async Task<ITransformer> Train(IEnumerable<BlossomVector> vectors)
    {
        // Convert BlossomVector to a class suitable for ML.NET
        var data = Load(vectors);

        // Normalize the vectors (L2 normalization)
        var pipeline = Context.Transforms.NormalizeLpNorm("Vector", norm: NormFunction.L2)
            .Append(Context.Clustering.Trainers.KMeans(
                featureColumnName: "Vector",
                numberOfClusters: 10));

        var model = pipeline.Fit(data);

        var kmeans = model.LastTransformer.Model;
        Console.WriteLine($"Cluster centroids:");
        VBuffer<float>[] centroids = [];
        kmeans.GetClusterCentroids(ref centroids, out int k);
        for (int i = 0; i < k; i++)
        {
            Console.WriteLine($"Centroid {i}: {string.Join(", ", centroids[i].DenseValues())}");
        }

        return model;
    }

    public async Task Transform(ITransformer transformer, IEnumerable<BlossomVector> vectors)
    {
        var data = Load(vectors);
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

public class ClusteringPrediction
{
    public string TargetUrl { get; set; } = "";
    public uint PredictedLabel { get; set; }
    public float[] Score { get; set; } = [];
}

