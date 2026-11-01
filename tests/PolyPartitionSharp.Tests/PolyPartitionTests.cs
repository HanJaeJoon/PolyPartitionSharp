using System.Numerics;

namespace PolyPartitionSharp.Tests;

public class PolyPartitionTests
{
    private static readonly Vector2[] Triangle = [new(0, 0), new(2, 0), new(0, 2)];
    private static readonly Vector2[] Square = [new(0, 0), new(2, 0), new(2, 2), new(0, 2)];
    private static readonly Vector2[] LShape = [new(0, 0), new(2, 0), new(2, 1), new(1, 1), new(1, 2), new(0, 2)];

    public static TheoryData<string, int, int> Shapes => new()
    {
        // name, vertex count, convex parts
        { nameof(Triangle), 3, 1 },
        { nameof(Square), 4, 1 },
        { nameof(LShape), 6, 2 },
    };

    [Theory]
    [MemberData(nameof(Shapes))]
    public void Should_ProduceNMinus2TrianglesWithSameArea_When_SimplePolygonIsTriangulated(string shape, int n, int _)
    {
        var polygon = new Polygon(Get(shape));

        Assert.True(PolyPartition.TryTriangulateEC(polygon, out List<Polygon> ec));
        Assert.True(PolyPartition.TryTriangulateOPT(polygon, out List<Polygon> opt));
        Assert.True(PolyPartition.TryTriangulateMONO(polygon, out List<Polygon> mono));

        foreach (List<Polygon> triangles in new[] { ec, opt, mono })
        {
            Assert.Equal(n - 2, triangles.Count);
            Assert.All(triangles, t => Assert.Equal(PolygonOrientation.CounterClockwise, t.GetOrientation()));
            Assert.Equal(Area(polygon), triangles.Sum(Area), 6);
        }
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public void Should_ProduceConvexPartsWithSameArea_When_SimplePolygonIsPartitioned(string shape, int _, int parts)
    {
        var polygon = new Polygon(Get(shape));

        Assert.True(PolyPartition.TryConvexPartitionHM(polygon, out List<Polygon> hm));
        Assert.Equal(parts, hm.Count);
        Assert.Equal(Area(polygon), hm.Sum(Area), 6);

        // Upstream ConvexPartition_OPT rejects a bare triangle: its closing edge fails the visibility check.
        bool optOk = PolyPartition.TryConvexPartitionOPT(polygon, out List<Polygon> opt);
        Assert.Equal(polygon.Count > 3, optOk);
        if (optOk)
        {
            Assert.Equal(parts, opt.Count);
            Assert.Equal(Area(polygon), opt.Sum(Area), 6);
        }
    }

    [Fact]
    public void Should_TriangulateAroundHole_When_PolygonHasOneHole()
    {
        var outer = new Polygon([new Vector2(0, 0), new Vector2(4, 0), new Vector2(4, 4), new Vector2(0, 4)]);
        var hole = new Polygon([new Vector2(1, 1), new Vector2(1, 3), new Vector2(3, 3), new Vector2(3, 1)], isHole: true);
        Polygon[] input = [outer, hole];

        Assert.True(PolyPartition.TryTriangulateEC(input, out List<Polygon> ec));
        Assert.True(PolyPartition.TryTriangulateMONO(input, out List<Polygon> mono));
        Assert.True(PolyPartition.TryConvexPartitionHM(input, out List<Polygon> hm));

        // n + 2h - 2 triangles for n vertices and h holes.
        Assert.Equal(8, ec.Count);
        Assert.Equal(8, mono.Count);
        Assert.Equal(12, ec.Sum(Area), 6);
        Assert.Equal(12, mono.Sum(Area), 6);
        Assert.Equal(12, hm.Sum(Area), 6);
    }

    [Fact]
    public void Should_CopyPointIds_When_Triangulating()
    {
        var polygon = new Polygon(Square.Select((v, i) => new PolygonPoint(v, 100 + i)));

        Assert.True(PolyPartition.TryTriangulateEC(polygon, out List<Polygon> triangles));

        Assert.All(triangles.SelectMany(t => t), p => Assert.Equal(Array.IndexOf(Square, p.Position) + 100, p.Id));
    }

    [Fact]
    public void Should_ReturnFalse_When_PolygonHasFewerThanThreePoints()
    {
        var polygon = new Polygon([new Vector2(0, 0), new Vector2(1, 0)]);

        Assert.False(PolyPartition.TryTriangulateEC(polygon, out _));
        Assert.False(PolyPartition.TryTriangulateOPT(polygon, out _));
        Assert.False(PolyPartition.TryTriangulateMONO(polygon, out _));
        Assert.False(PolyPartition.TryConvexPartitionHM(polygon, out _));
        Assert.False(PolyPartition.TryConvexPartitionOPT(polygon, out _));
    }

    [Fact]
    public void Should_ReverseClockwisePolygon_When_SetOrientationIsCounterClockwise()
    {
        var polygon = new Polygon(Enumerable.Reverse(Square));
        Assert.Equal(PolygonOrientation.Clockwise, polygon.GetOrientation());

        polygon.SetOrientation(PolygonOrientation.CounterClockwise);

        Assert.Equal(PolygonOrientation.CounterClockwise, polygon.GetOrientation());
    }

    private static Vector2[] Get(string shape)
    {
        return shape switch
        {
            nameof(Triangle) => Triangle,
            nameof(Square) => Square,
            _ => LShape,
        };
    }

    private static double Area(Polygon polygon)
    {
        double area = 0;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 a = polygon[i].Position;
            Vector2 b = polygon[(i + 1) % polygon.Count].Position;
            area += ((double)a.X * b.Y) - ((double)a.Y * b.X);
        }
        return area / 2;
    }
}
