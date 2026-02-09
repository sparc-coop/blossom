using MathNet.Numerics.LinearAlgebra;
using Sparc.Blossom.Spaces;
using System.Text;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Content;

// For schematization purposes for clustering model
public class BlossomVectorBase : BlossomEntity<string>
{
    public string SpaceId { get; init; } = "";
    public string Type { get; set; } = "Ephemeral";
    public string Model { get; init; } = "";
    public float[] Vector { get; set; } = [];
    public float[]? Point { get; set; }
    public double CoherenceWeight { get; set; } = 0;
    public double SimilarityToSpace { get; set; } = 0;
    public string? ConstellationId { get; set; }
    public string? ConstellationConnectorId { get; set; }
}

public class BlossomVector : BlossomVectorBase
{
    [JsonConstructor]
    public BlossomVector()
    {
    }

    public BlossomVector(BlossomSpace space, float[] vector)
        : this(space.Domain, space.RoomType, space.Id, vector)
    {
    }

    public BlossomVector(string spaceId, string type, string id, float[] vector) 
    {
        Id = id;
        SpaceId = spaceId;
        Type = type;
        Model = "text-embedding-3-small";
        Vector = vector;
        if (type == "Space")
            Point = new float[vector.Length];
    }

    public BlossomVector(string spaceId, float[] vector) 
        : this(spaceId, "Ephemeral", Guid.NewGuid().ToString(), vector)
    {
    }

    public BlossomVector(float[] vector) : this(Guid.NewGuid().ToString(), vector)
    {
    }

    public BlossomSummary? Summary { get; set; }
    public string? Text { get; set; }
    public bool IsEmpty => Vector.Length == 0 || Vector.All(x => x == 0);

    public void SetSummary(BlossomSummary? summary)
    {
        Summary = summary;
        if (Type == "Constellation")
            Text = summary?.Name;
    }

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

    public double SimilarityTo(BlossomVector other)
    {
        if (Vector.Length != other.Vector.Length)
            return 0;
        
        var dot = DotProduct(other);
        var magA = Magnitude();
        var magB = other.Magnitude();
        if (magA == 0 || magB == 0)
            return 0;
        return dot / (magA * magB);
    }

    public double? DistanceTo(BlossomVector other)
    {
        double sum = 0;
        for (int i = 0; i < Vector.Length; i++)
        {
            double diff = Vector[i] - other.Vector[i];
            sum += diff * diff;
        }
        return Math.Sqrt(sum);
    }

    public double PositionOnAxis(BlossomVector axis, double? axisMin = null, double? axisMax = null)
    {
        var rawPosition = DotProduct(axis);

        if (axisMin == null || axisMax == null)
            return rawPosition;

        var axisLength = axisMax.Value - axisMin.Value;
        if (axisLength == 0)
            return 0;

        return (rawPosition - axisMin.Value) / axisLength;
    }

    public BlossomVector Add(BlossomVector other)
    {
        if (IsEmpty)
            return ThisWith(other.Vector);
        
        var result = new float[Vector.Length];
        for (int i = 0; i < Vector.Length; i++)
            result[i] = Vector[i] + other.Vector[i];

        return ThisWith(result);
    }

    public BlossomVector Subtract(BlossomVector other)
    {
        var result = new float[Vector.Length];
        for (int i = 0; i < Vector.Length; i++)
            result[i] = Vector[i] - other.Vector[i];
        return ThisWith(result);
    }

    public BlossomVector Multiply(double scalar)
    {
        var result = new float[Vector.Length];
        for (int i = 0; i < Vector.Length; i++)
            result[i] = Vector[i] * (float)scalar;
        return ThisWith(result);
    }

    public double AlignmentWith(BlossomVector other)
    {
        var similarity = SimilarityTo(other);
        return Math.Abs(similarity);
    }

    public BlossomVector ThisWith(float[] other) => new(SpaceId, Type, Id, other) {  Text = Text };
    public double Length => Math.Sqrt(Vector.Sum(x => x * x));

    public void Update(BlossomVector vector, double scaleFactor = 1.0)
    {
        if (IsEmpty)
        {
            Point = vector.Vector;
            Vector = Normalize(Point);
        }
        else
        {
            Point = Multiply(1.0 - scaleFactor).Add(vector.Multiply(scaleFactor)).Vector;
            Vector = Normalize(Point);
        }
            
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

    public static BlossomVector Sum(IEnumerable<BlossomVector> spaceVectors)
    {
        var vectorLength = spaceVectors.First().Vector.Length;
        var sumVector = new float[vectorLength];
        foreach (var vec in spaceVectors)
        {
            for (int i = 0; i < vectorLength; i++)
            {
                sumVector[i] += vec.Vector[i];
            }
        }
        
        return new(spaceVectors.First().SpaceId, sumVector);
    }

    public static BlossomVector Average(IEnumerable<BlossomVector> spaceVectors, Func<BlossomVector, double>? weightingFunction = null)
    {
        var vectorLength = spaceVectors.First().Vector.Length;
        var avgVector = new float[vectorLength];
        foreach (var vec in spaceVectors)
        {
            for (int i = 0; i < vectorLength; i++)
            {
                avgVector[i] += vec.Vector[i] * (weightingFunction == null ? 1 : (float)weightingFunction(vec));
            }
        }

        var divisor = weightingFunction == null ? spaceVectors.Count() : spaceVectors.Sum(x => weightingFunction(x));
        for (int i = 0; i < vectorLength; i++)
        {
            avgVector[i] /= (float)divisor;
        }
        
        return new(spaceVectors.First().SpaceId, avgVector);
    }

    public BlossomVector Center(BlossomVector centerPoint)
    {
        var centeredVector = new float[Vector.Length];
        for (int i = 0; i < Vector.Length; i++)
            centeredVector[i] = Vector[i] - centerPoint.Vector[i];

        return ThisWith(centeredVector);
    }

    public static List<BlossomVector> ToAxes(BlossomVector answerVector, IEnumerable<BlossomVector> candidates)
    {
        var facets = candidates.Where(x => x.Type == "Facet")
            .OrderByDescending(x => x.CoherenceWeight)
            .Take(2)
            .ToList();

        var x = facets.FirstOrDefault() ?? Basis(1536, 0);
        var y = facets.Skip(1).FirstOrDefault() ?? Basis(1536, 1);
        var z = answerVector;

        x.Text = "X";
        y.Text = "Y";
        z?.Text = "Z";

        return z == null ? [x, y] : [x, y, z];
    }

    private Vector<float> ToMathNetVector() => Vector<float>.Build.Dense(Vector);
    public static Matrix<float> ToMatrix(List<BlossomVector> vectors)
        => Matrix<float>.Build.Dense(vectors.Count, vectors.First().Vector.Length, (i, j) => vectors[i].Vector[j]);

    public BlossomCoordinate ToCoordinate(List<BlossomVector> axes)
    {
        if (Vector.Length <= 3)
            return new BlossomCoordinate(
                Id, 
                Text ?? Id,
                Type,
                Vector.Length > 0 ? Vector[0] : 0,
                Vector.Length > 1 ? Vector[1] : 0,
                Vector.Length > 2 ? Vector[2] : 1)
            {
                Summary = Summary
            };

        var xAxis = axes.First();
        var yAxis = axes.Skip(1).FirstOrDefault();
        var zAxis = axes.Skip(2).FirstOrDefault();

        var x = PositionOnAxis(xAxis);
        var y = yAxis == null ? 0 : PositionOnAxis(yAxis);
        var z = zAxis == null ? 1 - DistanceFromPlane(xAxis, yAxis) : PositionOnAxis(zAxis);

        return new BlossomCoordinate(Id, Text ?? Id, Type, x, y, z)
        {
            Summary = Summary,
            ConnectTo = ConstellationConnectorId
        };
    }

    private BlossomVector Plane(BlossomVector xAxis, BlossomVector yAxis)
    {
        var plane = xAxis.Multiply(PositionOnAxis(xAxis)).Add(yAxis.Multiply(PositionOnAxis(yAxis)));
        return plane;
    }

    private double DistanceFromPlane(BlossomVector xAxis, BlossomVector yAxis)
    {
        var diff = Subtract(Plane(xAxis, yAxis)).Magnitude();
        return diff;
    }

    public BlossomVector Normalize()
    {
        var vec = ToMathNetVector();
        var normalized = vec.Normalize(2.0);
        return ThisWith([.. normalized]);
    }

    public float[] Normalize(float[] vector)
    {
        var vec = Vector<float>.Build.Dense(vector);
        var normalized = vec.Normalize(2.0);
        return [.. normalized];
    }

    internal void CalculateCoherenceWeight(List<BlossomVector> neighbors)
    {
        if (neighbors.Count == 0)
        { 
            CoherenceWeight = 1;
            return;
        }

        var sum = Sum(neighbors);
        var localSpace = sum.Normalize();
        var alignment = AlignmentWith(localSpace);

        // local agreement: weighted average of similarity(this, neighbor) using sim itself as the weight
        // also stabilize by neighbor.CoherenceWeight so strong neighbors contribute more
        double numer = 0;
        double denom = 0;
        foreach (var nb in neighbors)
        {
            var sim = nb.SimilarityTo(this);    // in [-1,1]
            var simPos = Math.Max(0.0, Math.Min(1.0, sim)); // clamp to [0,1]
            var weight = simPos * (1.0 + nb.CoherenceWeight); // neighbor coherence boosts influence
            numer += simPos * weight;
            denom += weight;
        }

        var localAgreement = denom == 0 ? 0 : numer / denom; // in [0,1]
        CoherenceWeight = alignment * localAgreement;
    }

    internal static BlossomVector Basis(int dimensions, int index)
    {
        var vec = new float[dimensions];
        vec[index] = 1;
        return new(vec);
    }

    internal BlossomVector ProjectOntoAxes(List<BlossomVector> axes)
    {
        var coordinate = ToCoordinate(axes);
        return ThisWith([(float)coordinate.X, (float)coordinate.Y]);
    }
}
