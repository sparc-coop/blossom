//using Microsoft.ML;
//using Microsoft.ML.Data;
//using Microsoft.ML.Trainers;
//using static Microsoft.ML.Transforms.LpNormNormalizingEstimatorBase;

//namespace Sparc.Blossom.Spaces;

//public class ClusteringPrediction
//{
//    public string TargetUrl { get; set; } = "";
//    public uint PredictedLabel { get; set; }
//    public float[] Score { get; set; } = [];
//}

//internal class BlossomSpaceClusterer()
//{
//    public MLContext Context { get; } = new MLContext(seed: 1);
//    private PredictionEngine<BlossomVector, ClusteringPrediction>? Predictor;

//    public async Task<List<BlossomVector>> ClusterAsync(BlossomSpace space, List<Post> posts, List<Axis> axes)
//    {
//        //var root = BlossomVector.Average(posts.Select(x => x.Vector));
//        //await vectors.UpdateAsync(root);

//        posts.ForEach(p => p.MaterializeCoordinates(axes));
//        var model = Cluster(posts);
//        Predictor = Context.Model.CreatePredictionEngine<BlossomVector, ClusteringPrediction>(model, inputSchemaDefinition: BlossomVectorSchema());
//        var clusterVectors = await CreateClusterVectors(space, model);
//        await AssignAsync(posts, clusterVectors, axes);

//        foreach (var vec in clusterVectors)
//            await vectors.SummarizeAsync(vec);

//        return clusterVectors;
//    }
    
//    public async Task AssignAsync(IEnumerable<Post> posts, List<BlossomVector> clusterVectors, List<BlossomVector> axes)
//    {
//        if (Predictor == null)
//            throw new InvalidOperationException("Model has not been trained. Please run RunAsync first.");

//        foreach (var post in posts.Where(x => x.Vector != null))
//        {
//            var prediction = Predictor.Predict(post.Vector.ProjectOntoAxes(axes));
//            var predictedCluster = clusterVectors[(int)prediction.PredictedLabel - 1];
//            post.Post.ConstellationId = predictedCluster.Id;
//        }
//    }

//    private TransformerChain<ClusteringPredictionTransformer<KMeansModelParameters>> Cluster(List<Post> posts)
//    {
//        var data = ToDataView(posts);
//        var kmeans = NormalizedKMeans(data, Math.Min(100, (int)Math.Sqrt(vectors.Count)));
//        return kmeans.Fit(data);
//    }

//    private async Task<List<BlossomVector>> CreateClusterVectors(BlossomSpace rootSpace, TransformerChain<ClusteringPredictionTransformer<KMeansModelParameters>> model)
//    {
//        await vectors.ClearAsync(rootSpace.Id, "Constellation");
//        var centroids = ExtractCentroids(model);
//        var result = centroids.Select(x => new BlossomVector(rootSpace.Id, "Constellation", Guid.NewGuid().ToString(), x)).ToList();
//        await vectors.UpdateAsync(result);
//        return result;
//    }

//    private EstimatorChain<ClusteringPredictionTransformer<KMeansModelParameters>> NormalizedKMeans(IDataView data, int maxSpaces = 100)
//    {
//        var scores = new List<double>();

//        for (var numSpaces = 1; numSpaces <= maxSpaces; numSpaces++)
//        {
//            try
//            {
//                var predictions = NormalizedKMeansModel(numSpaces).Fit(data).Transform(data);
//                var metrics = Context.Clustering.Evaluate(predictions);
//                scores.Add(metrics.AverageDistance);
//            }
//            catch
//            {
//                break;
//            }
//        }

//        var elbowK = FindElbow(scores, 1);
//        return NormalizedKMeansModel(elbowK);
//    }

//    private EstimatorChain<ClusteringPredictionTransformer<KMeansModelParameters>> NormalizedKMeansModel(int numSpaces)
//    {
//        var normalize = Context.Transforms.NormalizeLpNorm("Vector", norm: NormFunction.L2);
//        var options = new KMeansTrainer.Options
//        {
//            InitializationAlgorithm = KMeansTrainer.InitializationAlgorithm.KMeansPlusPlus,
//            FeatureColumnName = "Vector",
//            NumberOfClusters = numSpaces
//        };
//        var kmeans = Context.Clustering.Trainers.KMeans(options);
//        var pipeline = normalize.Append(kmeans);
//        return pipeline;
//    }

//    private static List<float[]> ExtractCentroids(TransformerChain<ClusteringPredictionTransformer<KMeansModelParameters>> model)
//    {
//        VBuffer<float>[] centroids = [];
//        model!.LastTransformer.Model.GetClusterCentroids(ref centroids, out int k);

//        List<float[]> newVectors = [];
//        for (int i = 0; i < k; i++)
//            newVectors.Add([.. centroids[i].DenseValues()]);

//        return newVectors;
//    }

//    private static int FindElbow(List<double> inertias, int minK = 1)
//    {
//        int n = inertias.Count;
//        // Normalize inertia values between 0 and 1
//        double maxVal = inertias.Max();
//        double minVal = inertias.Min();
//        List<double> norm = inertias.Select(v => (v - minVal) / (maxVal - minVal)).ToList();

//        // Create straight line from first to last point
//        List<double> line = [];
//        for (int i = 0; i < n; i++)
//        {
//            double frac = (double)i / (n - 1);
//            line.Add(1 - frac); // decreasing line from 1 to 0
//        }

//        // Compute difference between curve and line
//        List<double> diffs = [];
//        for (int i = 0; i < n; i++)
//        {
//            diffs.Add(line[i] - norm[i]);
//        }

//        // Find index of maximum deviation
//        int elbowIndex = diffs.IndexOf(diffs.Max());

//        // Adjust for minK offset
//        return minK + elbowIndex;
//    }

//    private async Task SaveModelAsync(IRepository<BlossomFile> files, ITransformer model, BlossomSpace space, DataViewSchema schema)
//    {
//        using var stream = new MemoryStream();
//        Context.Model.Save(model, schema, stream);

//        var file = new BlossomFile("models", $"{space.SpaceId}/{Guid.NewGuid()}.zip", AccessTypes.Private, stream);
//        await files.AddAsync(file);
//        space.ModelUrl = file.Url;
//    }

//    private static SchemaDefinition BlossomVectorSchema()
//    {
//        var schema = SchemaDefinition.Create(typeof(BlossomVectorBase), SchemaDefinition.Direction.Write);
//        schema[nameof(BlossomVector.Vector)].ColumnType = new VectorDataViewType(NumberDataViewType.Single, 2);
//        return schema;
//    }


//    private IDataView ToDataView(List<BlossomVector> vectors) => Context.Data.LoadFromEnumerable(vectors, BlossomVectorSchema());
//}
