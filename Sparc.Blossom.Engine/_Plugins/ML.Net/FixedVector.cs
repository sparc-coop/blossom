using Microsoft.ML.Data;
using Sparc.Blossom.Content;

namespace Sparc.Blossom.Plugins.MLNet;

public class FixedVector(string targetUrl, float[] vector)
{
    public string TargetUrl { get; set; } = targetUrl;
    [VectorType(1536)]
    public float[] Vector { get; set; } = vector;

    public static IEnumerable<FixedVector> From(IEnumerable<BlossomVector> vectors)
        => vectors.Select(x => new FixedVector(x.TargetUrl, x.Vector));
}

