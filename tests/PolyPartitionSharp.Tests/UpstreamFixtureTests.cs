using System.Globalization;
using System.Numerics;

namespace PolyPartitionSharp.Tests;

// Mirrors upstream test/test.cpp: run each algorithm on test_input.txt and compare to the expected file.
public class UpstreamFixtureTests
{
    [Fact]
    public void Should_MatchUpstream_When_TriangulateEC()
    {
        Assert.True(PolyPartition.TryTriangulateEC(ReadPolyList("test_input.txt"), out List<Polygon> result));
        AssertSame(ReadPolyList("test_triangulate_EC.txt"), result);
    }

    [Fact]
    public void Should_MatchUpstream_When_TriangulateOPT()
    {
        Assert.True(PolyPartition.TryTriangulateOPT(ReadPolyList("test_input.txt")[0], out List<Polygon> result));
        AssertSame(ReadPolyList("test_triangulate_OPT.txt"), result);
    }

    [Fact]
    public void Should_MatchUpstream_When_TriangulateMONO()
    {
        Assert.True(PolyPartition.TryTriangulateMONO(ReadPolyList("test_input.txt"), out List<Polygon> result));
        AssertSame(ReadPolyList("test_triangulate_MONO.txt"), result);
    }

    [Fact]
    public void Should_MatchUpstream_When_ConvexPartitionHM()
    {
        Assert.True(PolyPartition.TryConvexPartitionHM(ReadPolyList("test_input.txt"), out List<Polygon> result));
        AssertSame(ReadPolyList("test_convexpartition_HM.txt"), result);
    }

    [Fact]
    public void Should_MatchUpstream_When_ConvexPartitionOPT()
    {
        Assert.True(PolyPartition.TryConvexPartitionOPT(ReadPolyList("test_input.txt")[0], out List<Polygon> result));
        AssertSame(ReadPolyList("test_convexpartition_OPT.txt"), result);
    }

    // Same comparison as upstream ComparePoly: same polygon count, same points in the same order.
    private static void AssertSame(List<Polygon> expected, List<Polygon> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Select(p => p.Position), actual[i].Select(p => p.Position));
        }
    }

    private static List<Polygon> ReadPolyList(string name)
    {
        string[] tokens = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name))
            .Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        int t = 0;
        int Next() => int.Parse(tokens[t++], CultureInfo.InvariantCulture);
        float NextFloat() => float.Parse(tokens[t++], CultureInfo.InvariantCulture);

        var polys = new List<Polygon>();
        int numpolys = Next();
        for (int i = 0; i < numpolys; i++)
        {
            int numpoints = Next();
            bool hole = Next() != 0;
            var points = new Vector2[numpoints];
            for (int j = 0; j < numpoints; j++)
            {
                points[j] = new Vector2(NextFloat(), NextFloat());
            }
            polys.Add(new Polygon(points, hole));
        }
        return polys;
    }
}
