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
            Vector = Normalize().Vector;
    }

    public BlossomVector(float[] vector) : this(Guid.NewGuid().ToString(), "Ephemeral", vector)
    {
    }

    public string SpaceId { get; init; } = "";
    public string Type { get; init; } = "Ephemeral";
    public string Model { get; init; } = "";
    public float[] Vector { get; set; } = [];
    public float[]? Point { get; set; }
    public double Information { get; set; } = 1;
    public double Maturity { get; set; } = 1;
    public double Weight => Information * Maturity;
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

    public double? Score(BlossomVector axis)
    {
        if (axis.Point == null)
            return null;

        // Center the vector according to the axis point
        var centered = Center(new(axis.Point));

        return centered.SimilarityTo(axis);
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
        var point1 = Point ?? Vector;
        var point2 = other.Point ?? other.Vector;

        if (point1 == null || point2 == null)
            return null;

        double sum = 0;
        for (int i = 0; i < point1.Length; i++)
        {
            double diff = point1[i] - point2[i];
            sum += diff * diff;
        }
        return Math.Sqrt(sum);
    }

    public double? PositionOnAxis(BlossomVector axis, double? axisMin = null, double? axisMax = null)
    {
        if (axis.Point == null)
            return null;
        
        // Axis comes in already normalized, but needs to be centered
        var centered = Center(new(axis.Point));

        // projection of centered onto axis unit vector
        var rawPosition = centered.DotProduct(axis);

        if (axisMin == null || axisMax == null)
            return rawPosition;

        var axisLength = axisMax.Value - axisMin.Value;
        if (axisLength == 0)
            return null;

        return (rawPosition - axisMin.Value) / axisLength;
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

    public BlossomVector Center(BlossomVector centerPoint)
    {
        var centeredVector = new float[Vector.Length];
        for (int i = 0; i < Vector.Length; i++)
            centeredVector[i] = Vector[i] - centerPoint.Vector[i];

        return new BlossomVector(centeredVector);
    }

    private Vector<float> ToMathNetVector() => Vector<float>.Build.Dense(Vector);
    public static Matrix<float> ToMatrix(List<BlossomVector> vectors)
        => Matrix<float>.Build.Dense(vectors.Count, vectors.First().Vector.Length, (i, j) => vectors[i].Vector[j]);

    public static List<BlossomVector> ToPrincipalComponents(List<BlossomVector> vectors, double varianceToExplain)
    {
        var mean = Average(vectors);
        var centeredVectors = vectors.Select(v => v.Center(mean)).ToList();
        var matrix = ToMatrix(centeredVectors);

        var svd = matrix.Svd(true);
        var components = new List<BlossomVector>();
        for (int i = 0; i < svd.VT.RowCount; i++)
        {
            var componentArray = svd.VT.Row(i).ToArray();
            
            components.Add(new BlossomVector(vectors.First().SpaceId, "Facet", Guid.NewGuid().ToString(), componentArray)
            {
                Point = mean.Vector,
                Information = Math.Pow(svd.S[i], 2) / svd.S.Sum(x => x * x)
            });
            
            if (components.Sum(c => c.Information) >= varianceToExplain)
                break;
        }

        return components;
    }

    private BlossomVector Normalize()
    {
        var vec = ToMathNetVector();
        var normalized = vec.Normalize(2.0);
        return new(SpaceId, Type, Id, [.. normalized]);
    }

    internal double SetAnswer(IEnumerable<BlossomVector> previousVectors)
    {
        var prevAnswer = new BlossomVector(previousVectors.Last().Point!);
        var previousVectorsAndThis = previousVectors.Append(this).ToList();
        Point = CalculateAnswer(previousVectorsAndThis); // Provisional answer
        Information = DistanceTo(prevAnswer) ?? 0;

        foreach (var vector in previousVectors)
            vector.UpdateMaturity(this);

        // Calculate final answer using these weights
        Point = CalculateAnswer(previousVectorsAndThis);                                                                                   

        return Information;
    }

    private static float[] CalculateAnswer(List<BlossomVector> previousVectors)
    {
        var weightedVectors = previousVectors.Select(v => v.ToMathNetVector().Multiply((float)v.Weight)).ToList();

        var sumOfWeightedVectors = weightedVectors.Aggregate((a, b) => a + b);
        var totalWeight = previousVectors.Sum(v => v.Weight);
        var updatedVector = sumOfWeightedVectors.Divide((float)totalWeight);
        return [..updatedVector];
    }

    private void UpdateMaturity(BlossomVector currentAnswer)
    {
        // Exponential decay based on how far this vector's Point is from the current answer
        var distance = DistanceTo(currentAnswer);
        Maturity = distance == null ? 1.0 : Math.Exp(-distance.Value);
    }
}
