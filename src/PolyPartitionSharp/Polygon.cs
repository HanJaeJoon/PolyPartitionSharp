using System.Collections;
using System.Numerics;

namespace PolyPartitionSharp;

/// <summary>
/// A polygon: an array of points with a "hole" flag.
/// </summary>
public sealed class Polygon : IReadOnlyList<PolygonPoint>
{
    private readonly PolygonPoint[] _points;

    /// <summary>
    /// Creates a polygon from vertex positions. Every point gets id 0.
    /// </summary>
    /// <param name="vertices">The vertices, copied.</param>
    /// <param name="isHole">Whether the polygon is a hole.</param>
    /// <exception cref="ArgumentNullException"><paramref name="vertices"/> is null.</exception>
    public Polygon(IEnumerable<Vector2> vertices, bool isHole = false)
        : this((vertices ?? throw new ArgumentNullException(nameof(vertices))).Select(static v => new PolygonPoint(v)).ToArray(), isHole)
    {
    }

    /// <summary>
    /// Creates a polygon from points carrying user ids.
    /// </summary>
    /// <param name="points">The points, copied.</param>
    /// <param name="isHole">Whether the polygon is a hole.</param>
    /// <exception cref="ArgumentNullException"><paramref name="points"/> is null.</exception>
    public Polygon(IEnumerable<PolygonPoint> points, bool isHole = false)
        : this((points ?? throw new ArgumentNullException(nameof(points))).ToArray(), isHole)
    {
    }

    private Polygon(PolygonPoint[] points, bool isHole)
    {
        _points = points;
        IsHole = isHole;
    }

    /// <summary>Whether the polygon is a hole.</summary>
    public bool IsHole { get; set; }

    /// <summary>The number of points.</summary>
    public int Count => _points.Length;

    /// <summary>Whether the polygon has at least 3 points.</summary>
    public bool IsValid => _points.Length >= 3;

    /// <summary>Gets or sets the point at <paramref name="index"/>.</summary>
    public PolygonPoint this[int index]
    {
        get => _points[index];
        set => _points[index] = value;
    }

    /// <summary>
    /// Returns the orientation of the polygon, from the sign of its signed area.
    /// </summary>
    public PolygonOrientation GetOrientation()
    {
        int i1, i2;
        double area = 0;
        for (i1 = 0; i1 < _points.Length; i1++)
        {
            i2 = i1 + 1;
            if (i2 == _points.Length)
            {
                i2 = 0;
            }
            area += ((double)_points[i1].Position.X * _points[i2].Position.Y) - ((double)_points[i1].Position.Y * _points[i2].Position.X);
        }
        if (area > 0)
        {
            return PolygonOrientation.CounterClockwise;
        }
        if (area < 0)
        {
            return PolygonOrientation.Clockwise;
        }
        return PolygonOrientation.None;
    }

    /// <summary>
    /// Reverses the vertex order if the polygon has an orientation different from
    /// <paramref name="orientation"/>. A polygon with no area is left unchanged.
    /// </summary>
    public void SetOrientation(PolygonOrientation orientation)
    {
        PolygonOrientation polyorientation = GetOrientation();
        if (polyorientation != PolygonOrientation.None && polyorientation != orientation)
        {
            Invert();
        }
    }

    /// <summary>Reverses the order of the vertices.</summary>
    public void Invert()
    {
        Array.Reverse(_points);
    }

    /// <inheritdoc/>
    public IEnumerator<PolygonPoint> GetEnumerator()
    {
        return ((IEnumerable<PolygonPoint>)_points).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    internal PolygonPoint[] Points => _points;

    internal Polygon Clone()
    {
        return new Polygon((PolygonPoint[])_points.Clone(), IsHole);
    }

    internal static Polygon Wrap(PolygonPoint[] points)
    {
        return new Polygon(points, false);
    }

    internal static Polygon Triangle(PolygonPoint p1, PolygonPoint p2, PolygonPoint p3)
    {
        return new Polygon([p1, p2, p3], false);
    }
}
