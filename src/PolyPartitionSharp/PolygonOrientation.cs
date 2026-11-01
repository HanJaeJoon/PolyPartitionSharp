namespace PolyPartitionSharp;

/// <summary>
/// The winding order of a polygon's vertices.
/// </summary>
public enum PolygonOrientation
{
    /// <summary>Vertices are in clockwise order.</summary>
    Clockwise = -1,

    /// <summary>The polygon has no (measurable) area.</summary>
    None = 0,

    /// <summary>Vertices are in counter-clockwise order.</summary>
    CounterClockwise = 1,
}
