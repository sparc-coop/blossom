namespace Sparc.Blossom.Spaces;

public class ClusteringPrediction
{
    public string TargetUrl { get; set; } = "";
    public uint PredictedLabel { get; set; }
    public float[] Score { get; set; } = [];
}

