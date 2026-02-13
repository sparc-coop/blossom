using System.Text;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Spaces;

// For schematization purposes for clustering model
public class BlossomVectorBase
{
    public string Model { get; init; } = "";
    public float[] Vector { get; set; } = [];
    public float[]? Point { get; set; }
    public float CoherenceWeight { get; set; } = 0;
    public float SimilarityToSpace { get; set; } = 0;
    public string? ConstellationId { get; set; }
    public string? ConstellationConnectorId { get; set; }
}

public class BlossomVector : BlossomVectorBase
{
    [JsonConstructor]
    public BlossomVector()
    {
    }

    public BlossomVector(string text)
    {
        Text = text;
    }

    public BlossomVector(string model, float[] vector) 
    {
        Model = model;
        Vector = vector;
    }

    public BlossomVector(float[] vector)
    {
        Vector = vector;
    }

    public string? Text { get; set; }
    public bool IsEmpty => Vector.Length == 0 || Vector.All(x => x == 0);

    public float DotProduct(BlossomVector other)
    {
        var length = Math.Min(Vector.Length, other.Vector.Length);
        float dot = 0;
        for (int i = 0; i < length; i++)
            dot += Vector[i] * other.Vector[i];

        return dot;
    }

    public float Magnitude()
    {
        float sumSquares = 0;
        for (int i = 0; i < Vector.Length; i++)
            sumSquares += Vector[i] * Vector[i];

        return (float)Math.Sqrt(sumSquares);
    }

    public float SimilarityTo(BlossomVector other)
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

    public float DistanceTo(BlossomVector other)
    {
        float sum = 0;
        for (int i = 0; i < Vector.Length; i++)
        {
            float diff = Vector[i] - other.Vector[i];
            sum += diff * diff;
        }
        return (float)Math.Sqrt(sum);
    }

    public float PositionOnAxis(BlossomVector axis, float? axisMin = null, float? axisMax = null)
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

    public BlossomVector Multiply(float scalar)
    {
        var result = new float[Vector.Length];
        for (int i = 0; i < Vector.Length; i++)
            result[i] = Vector[i] * (float)scalar;
        return ThisWith(result);
    }

    public float AlignmentWith(BlossomVector other)
    {
        var similarity = SimilarityTo(other);
        return Math.Abs(similarity);
    }

    public BlossomVector ThisWith(float[] other, string? type = null) => new(other) {  Text = Text };
    public float Length => (float)Math.Sqrt(Vector.Sum(x => x * x));

    public void Update(BlossomVector vector, float scaleFactor = 1.0f)
    {
        if (IsEmpty)
        {
            Point = vector.Vector;
            Vector = new BlossomVector(Point).Normalize().Vector;
        }
        else
        {
            Point = Multiply(1.0f - scaleFactor).Add(vector.Multiply(scaleFactor)).Vector;
            Vector = new BlossomVector(Point).Normalize().Vector;
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
        
        return new(sumVector);
    }

    public static BlossomVector Average(IEnumerable<BlossomVector> spaceVectors, Func<BlossomVector, float>? weightingFunction = null)
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
        
        return new(avgVector);
    }

    public BlossomVector Center(BlossomVector centerPoint)
    {
        var centeredVector = new float[Vector.Length];
        for (int i = 0; i < Vector.Length; i++)
            centeredVector[i] = Vector[i] - centerPoint.Vector[i];

        return ThisWith(centeredVector);
    }

    

    private BlossomVector Plane(BlossomVector xAxis, BlossomVector yAxis)
    {
        var plane = xAxis.Multiply(PositionOnAxis(xAxis)).Add(yAxis.Multiply(PositionOnAxis(yAxis)));
        return plane;
    }

    public float DistanceFromPlane(BlossomVector xAxis, BlossomVector yAxis)
    {
        var diff = Subtract(Plane(xAxis, yAxis)).Magnitude();
        return diff;
    }

    public BlossomVector Normalize()
    {
        var magnitude = Magnitude();
        if (magnitude == 0)
            return ThisWith(Vector);

        return Multiply(1.0f / magnitude);
    }

    public BlossomVector Orthogonal()
    {
        var reference = Basis(Vector.Length, 0);
        var orthogonal = reference.Subtract(Multiply(DotProduct(reference))).Normalize();
        return orthogonal;
    }

    public BlossomVector Orthogonal(BlossomVector xAxis, BlossomVector yAxis)
    {
        var orthogonal = Subtract(xAxis.Multiply(DotProduct(xAxis))).Subtract(yAxis.Multiply(DotProduct(yAxis))).Normalize();
        return orthogonal;
    }

    public void CalculateCoherenceWeight(List<BlossomVector> neighbors)
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
        float numer = 0;
        float denom = 0;
        foreach (var nb in neighbors)
        {
            var sim = nb.SimilarityTo(this);    // in [-1,1]
            var simPos = Math.Max(0.0f, Math.Min(1.0f, sim)); // clamp to [0,1]
            var weight = simPos * (1.0f + nb.CoherenceWeight); // neighbor coherence boosts influence
            numer += simPos * weight;
            denom += weight;
        }

        var localAgreement = denom == 0 ? 0 : numer / denom; // in [0,1]
        CoherenceWeight = alignment * localAgreement;
    }

    public static BlossomVector Basis(int dimensions, int index)
    {
        var vec = new float[dimensions];
        vec[index] = 1;
        return new(vec);
    }

    public BlossomVector ToCoordinates(List<Axis> axes)
    {
        if (Vector.Length <= 3)
            return new(Vector);

        var xAxis = axes.First().Vector.Normalize();
        var yAxis = axes.Skip(1).FirstOrDefault()?.Vector.Normalize();
        var zAxis = axes.Skip(2).FirstOrDefault()?.Vector.Normalize();

        var x = PositionOnAxis(xAxis);
        var y = yAxis == null ? 0 : PositionOnAxis(yAxis);
        var z = zAxis == null
            ? yAxis == null
                ? AlignmentWith(xAxis)
                : 1 - DistanceFromPlane(xAxis, yAxis)
            : PositionOnAxis(zAxis);

        return new([x, y, z]);
    }

    public BlossomVector ProjectOntoPlane(BlossomVector xAxis, BlossomVector yAxis)
    {
        var plane = Plane(xAxis, yAxis);
        return plane.Normalize();
    }

    public BlossomVector Perpendicular(Axis xAxis, Axis yAxis)
    {
        var coordinate = ToCoordinates([xAxis, yAxis]);

        var x = coordinate.Vector[1] * -1;
        var y = coordinate.Vector[0];

        var result = xAxis.Vector.Multiply(x).Add(yAxis.Vector.Multiply(y));
        return result.Normalize();
    }
}
