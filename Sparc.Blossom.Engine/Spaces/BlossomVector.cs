using System.Text;
using System.Text.Json.Serialization;
using MathNet.Numerics.LinearAlgebra;

namespace Sparc.Blossom.Content;

public class BlossomVector : BlossomEntity<string>
{
    [JsonConstructor]
    public BlossomVector()
    {
    }

    public BlossomVector(string spaceId, string type, string id, float[] vector) 
        : base(id)
    {
        SpaceId = spaceId;
        Type = type;
        TargetUrl = id;
        Vector = vector;
    }

    public BlossomVector(string spaceId, string type, float[] vector) : base(spaceId)
    {
        SpaceId = spaceId;
        Type = type;
        TargetUrl = spaceId;
        Model = "text-embedding-3-small";
        Vector = vector;
        if (type == "Space")
            Normalize();
    }

    public BlossomVector(float[] vector) : this(Guid.NewGuid().ToString(), "Ephemeral", vector)
    {
    }

    public string SpaceId { get; init; } = "";
    public string Type { get; init; } = "Ephemeral";
    public string Model { get; init; } = "";
    public float[] Vector { get; set; } = [];
    public float[]? Point { get; private set; }
    public double? Weight { get; private set; }
    public string TargetUrl { get; set; } = "";
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

    public double? Score(BlossomVector axis)
    {
        if (axis.Point == null)
            return null;
        
        // Center the vector according to the axis point
        var centered = new float[Vector.Length];
        for (int i = 0; i < Vector.Length; i++)
            centered[i] = Vector[i] - axis.Vector[i];

        return SimilarityTo(new BlossomVector(centered));
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
        
        return new(spaceVectors.First().SpaceId, "Space", avgVector);
    }

    private Vector<float> ToMathNetVector() => Vector<float>.Build.Dense(Vector);
    public static Matrix<float> ToMatrix(List<BlossomVector> vectors)
        => Matrix<float>.Build.Dense(vectors.Count, vectors.First().Vector.Length, (i, j) => vectors[i].Vector[j]);

    public static List<BlossomVector> ToPrincipalComponents(List<BlossomVector> vectors, double varianceToExplain)
    {
        var mean = Average(vectors);
        var meanVector = mean.ToMathNetVector();
        var matrix = ToMatrix(vectors);

        // Subtract each vector by the mean
        for (int i = 0; i < matrix.RowCount; i++)
            matrix.SetRow(i, matrix.Row(i) - meanVector);

        var svd = matrix.Svd(true);
        var components = new List<BlossomVector>();
        for (int i = 0; i < svd.VT.RowCount; i++)
        {
            var componentArray = svd.VT.Row(i).ToArray();
            
            components.Add(new BlossomVector(vectors.First().SpaceId, "Facet", Guid.NewGuid().ToString(), componentArray)
            {
                Point = mean.Vector,
                Weight = Math.Pow(svd.S[i], 2) / svd.S.Sum(x => x * x)
            });
            
            if (components.Sum(c => c.Weight) >= varianceToExplain)
                break;
        }

        return components;
    }

    private void Normalize()
    {
        Point = Vector;
        var vec = ToMathNetVector();
        var normalized = vec.Normalize(vec.L2Norm());
        Vector = [.. normalized];
    }
}
