using System.Text;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Content;

public class BlossomVector : BlossomEntity<string>
{
    [JsonConstructor]
    protected BlossomVector()
    {
    }

    public BlossomVector(string spaceId, string model, float[] vector, string targetUrl) 
        : base(Guid.NewGuid().ToString())
    {
        SpaceId = spaceId;
        Model = model;
        Vector = vector;
        TargetUrl = targetUrl;
    }

    public BlossomVector(float[] vector)
    {
        Vector = vector;
    }

    public string SpaceId { get; init; } = "";
    public string Model { get; init; } = "";
    public float[] Vector { get; init; } = [];
    public string TargetUrl { get; init; } = "";
    public string? Text { get; set; }

    public double DotProduct(float[] other)
    {
        var length = Math.Min(Vector.Length, other.Length);
        double dot = 0;
        for (int i = 0; i < length; i++)
            dot += Vector[i] * other[i];

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

    public double DirectionFrom(float[] other)
    {
        if (Vector.Length != other.Length)
            throw new ArgumentException("Vectors must be of the same length to calculate direction.");
        
        var dot = DotProduct(other);
        var magA = Magnitude();
        var magB = new BlossomVector(other).Magnitude();
        if (magA == 0 || magB == 0)
            return 0;
        return Math.Acos(dot / (magA * magB));
    }

    public double DistanceTo(float[] other)
    {
        if (Vector.Length != other.Length)
            throw new ArgumentException("Vectors must be of the same length to calculate distance.");
        double sum = 0;
        for (int i = 0; i < Vector.Length; i++)
        {
            double diff = Vector[i] - other[i];
            sum += diff * diff;
        }
        return Math.Sqrt(sum);
    }

    public double DissentFrom(float[] other) => DirectionFrom(other) / Math.PI;

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

    public static float[] Average(IEnumerable<BlossomVector> spaceVectors)
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
        return avgVector;
    }
}
