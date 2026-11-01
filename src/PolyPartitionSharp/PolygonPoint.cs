using System.Numerics;

namespace PolyPartitionSharp;

/// <summary>
/// A polygon vertex: a position plus a user-specified identifier.
/// </summary>
/// <param name="position">The vertex position.</param>
/// <param name="id">
/// User-specified vertex identifier. It is not used by the library, but is faithfully copied
/// into every output polygon, so callers can map output vertices back to their input.
/// </param>
public readonly struct PolygonPoint(Vector2 position, int id = 0)
{
    /// <summary>The vertex position.</summary>
    public Vector2 Position { get; } = position;

    /// <summary>The user-specified vertex identifier.</summary>
    public int Id { get; } = id;

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Position} (id {Id})";
    }
}
