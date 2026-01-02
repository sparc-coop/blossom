using System.Text;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Content;

public class BlossomVector : BlossomEntity<string>
{
    [JsonConstructor]
    public BlossomVector()
    {
    }

    public BlossomVector(string spaceId, string id, string model, float[] vector) 
        : base(id)
    {
        SpaceId = spaceId;
        Model = model;
        Vector = vector;
    }

    public BlossomVector(string spaceId, float[] vector) : base(spaceId)
    {
        SpaceId = spaceId;
        TargetUrl = spaceId;
        Model = "text-embedding-3-small";
        Vector = vector;
    }

    public BlossomVector(float[] vector) : this(Guid.NewGuid().ToString(), vector)
    {
    }

    public string SpaceId { get; init; } = "";
    public string Model { get; init; } = "";
    public float[] Vector { get; set; } = [];
    public float[]? Point { get; set; }
    public string TargetUrl { get; init; } = "";
    public string? Text { get; set; }

    public double DotProduct(BlossomVector other)
    {
        var length = Math.Min(Vector.Length, other.Vector.Length);
        double dot = 0;
        for (int i = 0; i < length; i++)
            dot += Vector[i] * other.Vector[i];

        return dot;
    }

    public double Magnitude()
    {
        double sumSquares = 0;
        for (int i = 0; i < Vector.Length; i++)
            sumSquares += Vector[i] * Vector[i];

        return Math.Sqrt(sumSquares);
    }

    public double Direction()
    {
        double angle = 0;
        double sumSquares = 0;
        for (int i = 0; i < Vector.Length; i++)
        {
            angle += Vector[i];
            sumSquares += Vector[i] * Vector[i];
        }
        if (sumSquares == 0)
            return 0;
        return Math.Acos(angle / Math.Sqrt(sumSquares));
    }

    public double? SimilarityTo(BlossomVector other)
    {
        if (Vector.Length != other.Vector.Length)
            return null;
        
        var dot = DotProduct(other);
        var magA = Magnitude();
        var magB = other.Magnitude();
        if (magA == 0 || magB == 0)
            return 0;
        return dot / (magA * magB);
    }

    public double? DistanceTo(BlossomVector other)
    {
        if (Point == null && other.Point == null)
            return null;

        var point1 = Point ?? Vector;
        var point2 = other.Point ?? other.Vector;

        double sum = 0;
        for (int i = 0; i < point1.Length; i++)
        {
            double diff = point1[i] - point2[i];
            sum += diff * diff;
        }
        return Math.Sqrt(sum);
    }

    public double? ClosenessTo(BlossomVector other)
    {
        var distance = DistanceTo(other);
        if (distance == null)
            return null;

        return Math.Pow(Math.E, -1 * distance.Value);
    }
    public double? AlignmentWith(BlossomVector other)
    {
        var similarity = SimilarityTo(other);
        return similarity == null ? null : Math.Abs(similarity.Value);
    }

    public double? DissentFrom(BlossomVector other)
    {
        var similarity = SimilarityTo(other);
        return similarity == null ? null : (1.0 - similarity.Value) / 2.0;
    }

    public override string ToString()
    {
        var str = new StringBuilder();
        str.Append('[');
        for (int i = 0; i < Vector.Length; i++)
        {
            str.Append(Vector[i]);
            if (i < Vector.Length - 1)
                str.Append(',');
        }
        str.Append(']');
        return str.ToString();
    }

    public static BlossomVector Average(IEnumerable<BlossomVector> spaceVectors)
    {
        var vectorLength = spaceVectors.First().Vector.Length;
        var count = spaceVectors.Count();
        var avgVector = new float[vectorLength];
        foreach (var vec in spaceVectors)
        {
            for (int i = 0; i < vectorLength; i++)
            {
                avgVector[i] += vec.Vector[i];
            }
        }
        for (int i = 0; i < vectorLength; i++)
        {
            avgVector[i] /= count;
        }
        
        return new(spaceVectors.First().SpaceId, avgVector);
    }

    public void SetMean(List<BlossomVector> spaceVectors)
    {
        Point = Average(spaceVectors).Vector;
    }
}
