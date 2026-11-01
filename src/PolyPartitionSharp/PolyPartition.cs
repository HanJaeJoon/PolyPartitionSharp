using System.Numerics;

namespace PolyPartitionSharp;

/// <summary>
/// Polygon triangulation and convex partitioning algorithms, ported from
/// polypartition (https://github.com/ivanfratric/polypartition).
/// </summary>
/// <remarks>
/// Every method mirrors the upstream <c>int</c> return: it returns <see langword="true"/> on success
/// and <see langword="false"/> on failure. On failure the output list holds whatever was produced
/// before the failure, as upstream leaves it. Output points are copies of input points, ids included.
/// All geometry is evaluated in double precision.
/// </remarks>
public static partial class PolyPartition
{
    /// <summary>
    /// Simple heuristic procedure for removing holes from a list of polygons.
    /// It works by creating a diagonal from the right-most hole vertex to some other visible vertex.
    /// Time complexity: O(h*(n^2)), h is the # of holes, n is the # of vertices.
    /// Space complexity: O(n).
    /// </summary>
    /// <param name="polygons">
    /// A list of polygons that can contain holes. Vertices of all non-hole polygons have to be in
    /// counter-clockwise order. Vertices of all hole polygons have to be in clockwise order.
    /// </param>
    /// <param name="result">A list of polygons without holes.</param>
    public static bool TryRemoveHoles(IEnumerable<Polygon> polygons, out List<Polygon> result)
    {
        result = [];
        return RemoveHoles(ToList(polygons), result);
    }

    /// <summary>
    /// Triangulates a polygon by ear clipping.
    /// Time complexity: O(n^2), n is the number of vertices. Space complexity: O(n).
    /// </summary>
    /// <param name="polygon">An input polygon to be triangulated. Vertices have to be in counter-clockwise order.</param>
    /// <param name="triangles">A list of triangles (result).</param>
    public static bool TryTriangulateEC(Polygon polygon, out List<Polygon> triangles)
    {
        triangles = [];
        return TriangulateEC(NotNull(polygon), triangles);
    }

    /// <summary>
    /// Triangulates a list of polygons that may contain holes by ear clipping. It first calls
    /// <see cref="TryRemoveHoles"/> to get rid of the holes, and then triangulates each resulting polygon.
    /// Time complexity: O(h*(n^2)), h is the # of holes, n is the # of vertices. Space complexity: O(n).
    /// </summary>
    /// <param name="polygons">
    /// A list of polygons to be triangulated (can contain holes). Vertices of all non-hole polygons
    /// have to be in counter-clockwise order. Vertices of all hole polygons have to be in clockwise order.
    /// </param>
    /// <param name="triangles">A list of triangles (result).</param>
    public static bool TryTriangulateEC(IEnumerable<Polygon> polygons, out List<Polygon> triangles)
    {
        triangles = [];
        return TriangulateEC(ToList(polygons), triangles);
    }

    /// <summary>
    /// Creates an optimal polygon triangulation in terms of minimal edge length.
    /// Time complexity: O(n^3), n is the number of vertices. Space complexity: O(n^2).
    /// </summary>
    /// <param name="polygon">An input polygon to be triangulated. Vertices have to be in counter-clockwise order.</param>
    /// <param name="triangles">A list of triangles (result).</param>
    public static bool TryTriangulateOPT(Polygon polygon, out List<Polygon> triangles)
    {
        triangles = [];
        return TriangulateOPT(NotNull(polygon), triangles);
    }

    /// <summary>
    /// Triangulates a polygon by first partitioning it into monotone polygons.
    /// Time complexity: O(n*log(n)), n is the number of vertices. Space complexity: O(n).
    /// </summary>
    /// <param name="polygon">An input polygon to be triangulated. Vertices have to be in counter-clockwise order.</param>
    /// <param name="triangles">A list of triangles (result).</param>
    public static bool TryTriangulateMONO(Polygon polygon, out List<Polygon> triangles)
    {
        triangles = [];
        return TriangulateMONO([NotNull(polygon)], triangles);
    }

    /// <summary>
    /// Triangulates a list of polygons by first partitioning them into monotone polygons.
    /// Time complexity: O(n*log(n)), n is the number of vertices. Space complexity: O(n).
    /// </summary>
    /// <param name="polygons">
    /// A list of polygons to be triangulated (can contain holes). Vertices of all non-hole polygons
    /// have to be in counter-clockwise order. Vertices of all hole polygons have to be in clockwise order.
    /// </param>
    /// <param name="triangles">A list of triangles (result).</param>
    public static bool TryTriangulateMONO(IEnumerable<Polygon> polygons, out List<Polygon> triangles)
    {
        triangles = [];
        return TriangulateMONO(ToList(polygons), triangles);
    }

    /// <summary>
    /// Creates a monotone partition of a list of polygons that can contain holes.
    /// Time complexity: O(n*log(n)), n is the number of vertices. Space complexity: O(n).
    /// </summary>
    /// <param name="polygons">
    /// A list of polygons to be partitioned (can contain holes). Vertices of all non-hole polygons
    /// have to be in counter-clockwise order. Vertices of all hole polygons have to be in clockwise order.
    /// </param>
    /// <param name="monotonePolygons">A list of monotone polygons (result).</param>
    public static bool TryMonotonePartition(IEnumerable<Polygon> polygons, out List<Polygon> monotonePolygons)
    {
        monotonePolygons = [];
        return MonotonePartition(ToList(polygons), monotonePolygons);
    }

    /// <summary>
    /// Partitions a polygon into convex polygons by using the Hertel-Mehlhorn algorithm. The algorithm
    /// gives at most four times the number of parts as the optimal algorithm, however, in practice it
    /// works much better than that and often gives optimal partition. It uses triangulation obtained
    /// by ear clipping as intermediate result.
    /// Time complexity: O(n^2), n is the number of vertices. Space complexity: O(n).
    /// </summary>
    /// <param name="polygon">An input polygon to be partitioned. Vertices have to be in counter-clockwise order.</param>
    /// <param name="parts">Resulting list of convex polygons.</param>
    public static bool TryConvexPartitionHM(Polygon polygon, out List<Polygon> parts)
    {
        parts = [];
        return ConvexPartitionHM(NotNull(polygon), parts);
    }

    /// <summary>
    /// Partitions a list of polygons into convex parts by using the Hertel-Mehlhorn algorithm.
    /// It first calls <see cref="TryRemoveHoles"/> to get rid of the holes.
    /// Time complexity: O(n^2), n is the number of vertices. Space complexity: O(n).
    /// </summary>
    /// <param name="polygons">
    /// An input list of polygons to be partitioned. Vertices of all non-hole polygons have to be in
    /// counter-clockwise order. Vertices of all hole polygons have to be in clockwise order.
    /// </param>
    /// <param name="parts">Resulting list of convex polygons.</param>
    public static bool TryConvexPartitionHM(IEnumerable<Polygon> polygons, out List<Polygon> parts)
    {
        parts = [];
        return ConvexPartitionHM(ToList(polygons), parts);
    }

    /// <summary>
    /// Optimal convex partitioning (in terms of number of resulting convex polygons) using the
    /// Keil-Snoeyink algorithm. For reference, see M. Keil, J. Snoeyink, "On the time bound for
    /// convex decomposition of simple polygons", 1998.
    /// Time complexity: O(n^3), n is the number of vertices. Space complexity: O(n^3).
    /// As upstream, a polygon with only 3 vertices is rejected (returns <see langword="false"/>).
    /// </summary>
    /// <param name="polygon">An input polygon to be partitioned. Vertices have to be in counter-clockwise order.</param>
    /// <param name="parts">Resulting list of convex polygons.</param>
    public static bool TryConvexPartitionOPT(Polygon polygon, out List<Polygon> parts)
    {
        parts = [];
        return ConvexPartitionOPT(NotNull(polygon), parts);
    }

    private static Polygon NotNull(Polygon polygon)
    {
        return polygon ?? throw new ArgumentNullException(nameof(polygon));
    }

    private static List<Polygon> ToList(IEnumerable<Polygon> polygons)
    {
        if (polygons is null)
        {
            throw new ArgumentNullException(nameof(polygons));
        }

        var list = new List<Polygon>();
        foreach (Polygon polygon in polygons)
        {
            list.Add(polygon ?? throw new ArgumentException("The list contains a null polygon.", nameof(polygons)));
        }
        return list;
    }

    private sealed class PartitionVertex
    {
        public bool isActive;
        public bool isConvex;
        public bool isEar;

        public PolygonPoint p;
        public double angle;
        public PartitionVertex previous = null!;
        public PartitionVertex next = null!;
    }

    private struct Diagonal(int index1, int index2)
    {
        public int index1 = index1;
        public int index2 = index2;
    }

    // Standard helper functions.

    private static bool IsConvex(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        double tmp = (((double)p3.Y - p1.Y) * ((double)p2.X - p1.X)) - (((double)p3.X - p1.X) * ((double)p2.Y - p1.Y));
        return tmp > 0;
    }

    private static bool IsReflex(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        double tmp = (((double)p3.Y - p1.Y) * ((double)p2.X - p1.X)) - (((double)p3.X - p1.X) * ((double)p2.Y - p1.Y));
        return tmp < 0;
    }

    private static bool IsInside(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p)
    {
        if (IsConvex(p1, p, p2))
        {
            return false;
        }
        if (IsConvex(p2, p, p3))
        {
            return false;
        }
        if (IsConvex(p3, p, p1))
        {
            return false;
        }
        return true;
    }

    private static bool InCone(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p)
    {
        bool convex = IsConvex(p1, p2, p3);

        if (convex)
        {
            if (!IsConvex(p1, p2, p))
            {
                return false;
            }
            if (!IsConvex(p2, p3, p))
            {
                return false;
            }
            return true;
        }
        else
        {
            if (IsConvex(p1, p2, p))
            {
                return true;
            }
            if (IsConvex(p2, p3, p))
            {
                return true;
            }
            return false;
        }
    }

    private static bool InCone(PartitionVertex v, Vector2 p)
    {
        return InCone(v.previous.p.Position, v.p.Position, v.next.p.Position, p);
    }

    // Checks if two lines intersect.
    private static bool Intersects(Vector2 p11, Vector2 p12, Vector2 p21, Vector2 p22)
    {
        if ((p11.X == p21.X) && (p11.Y == p21.Y))
        {
            return false;
        }
        if ((p11.X == p22.X) && (p11.Y == p22.Y))
        {
            return false;
        }
        if ((p12.X == p21.X) && (p12.Y == p21.Y))
        {
            return false;
        }
        if ((p12.X == p22.X) && (p12.Y == p22.Y))
        {
            return false;
        }

        double v1ortx = (double)p12.Y - p11.Y;
        double v1orty = (double)p11.X - p12.X;

        double v2ortx = (double)p22.Y - p21.Y;
        double v2orty = (double)p21.X - p22.X;

        double dot21 = (((double)p21.X - p11.X) * v1ortx) + (((double)p21.Y - p11.Y) * v1orty);
        double dot22 = (((double)p22.X - p11.X) * v1ortx) + (((double)p22.Y - p11.Y) * v1orty);

        double dot11 = (((double)p11.X - p21.X) * v2ortx) + (((double)p11.Y - p21.Y) * v2orty);
        double dot12 = (((double)p12.X - p21.X) * v2ortx) + (((double)p12.Y - p21.Y) * v2orty);

        if (dot11 * dot12 > 0)
        {
            return false;
        }
        if (dot21 * dot22 > 0)
        {
            return false;
        }

        return true;
    }

    private static void Normalize(double x, double y, out double rx, out double ry)
    {
        double n = Math.Sqrt((x * x) + (y * y));
        if (n != 0)
        {
            rx = x / n;
            ry = y / n;
        }
        else
        {
            rx = 0;
            ry = 0;
        }
    }

    private static double Distance(Vector2 p1, Vector2 p2)
    {
        double dx = (double)p2.X - p1.X;
        double dy = (double)p2.Y - p1.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    // Removes holes from inpolys by merging them with non-holes.
    private static bool RemoveHoles(List<Polygon> inpolys, List<Polygon> outpolys)
    {
        int holeiter = 0, polyiter = 0, iter, iter2;
        int i, i2, holepointindex = 0, polypointindex = 0;
        Vector2 holepoint, polypoint, bestpolypoint = default;
        bool hasholes;
        bool pointvisible;
        bool pointfound;

        // Check for the trivial case of no holes.
        hasholes = false;
        for (iter = 0; iter < inpolys.Count; iter++)
        {
            if (inpolys[iter].IsHole)
            {
                hasholes = true;
                break;
            }
        }
        if (!hasholes)
        {
            for (iter = 0; iter < inpolys.Count; iter++)
            {
                outpolys.Add(inpolys[iter].Clone());
            }
            return true;
        }

        var polys = new List<Polygon>(inpolys);

        while (true)
        {
            // Find the hole point with the largest x.
            hasholes = false;
            for (iter = 0; iter < polys.Count; iter++)
            {
                if (!polys[iter].IsHole)
                {
                    continue;
                }

                if (!hasholes)
                {
                    hasholes = true;
                    holeiter = iter;
                    holepointindex = 0;
                }

                for (i = 0; i < polys[iter].Count; i++)
                {
                    if (polys[iter][i].Position.X > polys[holeiter][holepointindex].Position.X)
                    {
                        holeiter = iter;
                        holepointindex = i;
                    }
                }
            }
            if (!hasholes)
            {
                break;
            }
            holepoint = polys[holeiter][holepointindex].Position;

            pointfound = false;
            for (iter = 0; iter < polys.Count; iter++)
            {
                Polygon poly = polys[iter];
                if (poly.IsHole)
                {
                    continue;
                }
                for (i = 0; i < poly.Count; i++)
                {
                    if (poly[i].Position.X <= holepoint.X)
                    {
                        continue;
                    }
                    if (!InCone(poly[(i + poly.Count - 1) % poly.Count].Position,
                                poly[i].Position,
                                poly[(i + 1) % poly.Count].Position,
                                holepoint))
                    {
                        continue;
                    }
                    polypoint = poly[i].Position;
                    if (pointfound)
                    {
                        double v1dist = Distance(holepoint, polypoint);
                        double v2dist = Distance(holepoint, bestpolypoint);
                        if (v2dist < v1dist)
                        {
                            continue;
                        }
                    }
                    pointvisible = true;
                    for (iter2 = 0; iter2 < polys.Count; iter2++)
                    {
                        Polygon poly2 = polys[iter2];
                        if (poly2.IsHole)
                        {
                            continue;
                        }
                        for (i2 = 0; i2 < poly2.Count; i2++)
                        {
                            Vector2 linep1 = poly2[i2].Position;
                            Vector2 linep2 = poly2[(i2 + 1) % poly2.Count].Position;
                            if (Intersects(holepoint, polypoint, linep1, linep2))
                            {
                                pointvisible = false;
                                break;
                            }
                        }
                        if (!pointvisible)
                        {
                            break;
                        }
                    }
                    if (pointvisible)
                    {
                        pointfound = true;
                        bestpolypoint = polypoint;
                        polyiter = iter;
                        polypointindex = i;
                    }
                }
            }

            if (!pointfound)
            {
                return false;
            }

            Polygon hole = polys[holeiter];
            Polygon outer = polys[polyiter];
            var newpoly = new PolygonPoint[hole.Count + outer.Count + 2];
            i2 = 0;
            for (i = 0; i <= polypointindex; i++)
            {
                newpoly[i2] = outer[i];
                i2++;
            }
            for (i = 0; i <= hole.Count; i++)
            {
                newpoly[i2] = hole[(i + holepointindex) % hole.Count];
                i2++;
            }
            for (i = polypointindex; i < outer.Count; i++)
            {
                newpoly[i2] = outer[i];
                i2++;
            }

            polys.RemoveAt(Math.Max(holeiter, polyiter));
            polys.RemoveAt(Math.Min(holeiter, polyiter));
            polys.Add(Polygon.Wrap(newpoly));
        }

        for (iter = 0; iter < polys.Count; iter++)
        {
            outpolys.Add(polys[iter].Clone());
        }

        return true;
    }

    private static void UpdateVertexReflexity(PartitionVertex v)
    {
        PartitionVertex v1 = v.previous;
        PartitionVertex v3 = v.next;
        v.isConvex = !IsReflex(v1.p.Position, v.p.Position, v3.p.Position);
    }

    private static void UpdateVertex(PartitionVertex v, PartitionVertex[] vertices, int numvertices)
    {
        int i;
        PartitionVertex v1 = v.previous;
        PartitionVertex v3 = v.next;
        Vector2 vp = v.p.Position;
        Vector2 v1p = v1.p.Position;
        Vector2 v3p = v3.p.Position;

        v.isConvex = IsConvex(v1p, vp, v3p);

        Normalize((double)v1p.X - vp.X, (double)v1p.Y - vp.Y, out double vec1x, out double vec1y);
        Normalize((double)v3p.X - vp.X, (double)v3p.Y - vp.Y, out double vec3x, out double vec3y);
        v.angle = (vec1x * vec3x) + (vec1y * vec3y);

        if (v.isConvex)
        {
            v.isEar = true;
            for (i = 0; i < numvertices; i++)
            {
                Vector2 p = vertices[i].p.Position;
                if ((p.X == vp.X) && (p.Y == vp.Y))
                {
                    continue;
                }
                if ((p.X == v1p.X) && (p.Y == v1p.Y))
                {
                    continue;
                }
                if ((p.X == v3p.X) && (p.Y == v3p.Y))
                {
                    continue;
                }
                if (IsInside(v1p, vp, v3p, p))
                {
                    v.isEar = false;
                    break;
                }
            }
        }
        else
        {
            v.isEar = false;
        }
    }

    // Triangulation by ear removal.
    private static bool TriangulateEC(Polygon poly, List<Polygon> triangles)
    {
        if (!poly.IsValid)
        {
            return false;
        }

        int numvertices;
        PartitionVertex ear = null!;
        int i, j;
        bool earfound;

        if (poly.Count == 3)
        {
            triangles.Add(poly.Clone());
            return true;
        }

        numvertices = poly.Count;

        var vertices = new PartitionVertex[numvertices];
        for (i = 0; i < numvertices; i++)
        {
            vertices[i] = new PartitionVertex();
        }
        for (i = 0; i < numvertices; i++)
        {
            vertices[i].isActive = true;
            vertices[i].p = poly[i];
            vertices[i].next = i == (numvertices - 1) ? vertices[0] : vertices[i + 1];
            vertices[i].previous = i == 0 ? vertices[numvertices - 1] : vertices[i - 1];
        }
        for (i = 0; i < numvertices; i++)
        {
            UpdateVertex(vertices[i], vertices, numvertices);
        }

        for (i = 0; i < numvertices - 3; i++)
        {
            earfound = false;
            // Find the most extruded ear.
            for (j = 0; j < numvertices; j++)
            {
                if (!vertices[j].isActive)
                {
                    continue;
                }
                if (!vertices[j].isEar)
                {
                    continue;
                }
                if (!earfound)
                {
                    earfound = true;
                    ear = vertices[j];
                }
                else
                {
                    if (vertices[j].angle > ear.angle)
                    {
                        ear = vertices[j];
                    }
                }
            }
            if (!earfound)
            {
                return false;
            }

            triangles.Add(Polygon.Triangle(ear.previous.p, ear.p, ear.next.p));

            ear.isActive = false;
            ear.previous.next = ear.next;
            ear.next.previous = ear.previous;

            if (i == numvertices - 4)
            {
                break;
            }

            UpdateVertex(ear.previous, vertices, numvertices);
            UpdateVertex(ear.next, vertices, numvertices);
        }
        for (i = 0; i < numvertices; i++)
        {
            if (vertices[i].isActive)
            {
                triangles.Add(Polygon.Triangle(vertices[i].previous.p, vertices[i].p, vertices[i].next.p));
                break;
            }
        }

        return true;
    }

    private static bool TriangulateEC(List<Polygon> inpolys, List<Polygon> triangles)
    {
        var outpolys = new List<Polygon>();

        if (!RemoveHoles(inpolys, outpolys))
        {
            return false;
        }
        foreach (Polygon poly in outpolys)
        {
            if (!TriangulateEC(poly, triangles))
            {
                return false;
            }
        }
        return true;
    }

    private static bool ConvexPartitionHM(Polygon poly, List<Polygon> parts)
    {
        if (!poly.IsValid)
        {
            return false;
        }

        var triangles = new List<Polygon>();
        int iter1, iter2;
        Polygon poly1;
        Polygon poly2 = null!;
        PolygonPoint d1, d2;
        Vector2 p1, p2, p3;
        int i11, i12, i21 = 0, i22 = 0, i13, i23, j, k;
        bool isdiagonal;
        int numreflex;

        // Check if the poly is already convex.
        numreflex = 0;
        for (i11 = 0; i11 < poly.Count; i11++)
        {
            i12 = i11 == 0 ? poly.Count - 1 : i11 - 1;
            i13 = i11 == (poly.Count - 1) ? 0 : i11 + 1;
            if (IsReflex(poly[i12].Position, poly[i11].Position, poly[i13].Position))
            {
                numreflex = 1;
                break;
            }
        }
        if (numreflex == 0)
        {
            parts.Add(poly.Clone());
            return true;
        }

        if (!TriangulateEC(poly, triangles))
        {
            return false;
        }

        for (iter1 = 0; iter1 < triangles.Count; iter1++)
        {
            poly1 = triangles[iter1];
            for (i11 = 0; i11 < poly1.Count; i11++)
            {
                d1 = poly1[i11];
                i12 = (i11 + 1) % poly1.Count;
                d2 = poly1[i12];

                isdiagonal = false;
                for (iter2 = iter1; iter2 < triangles.Count; iter2++)
                {
                    if (iter1 == iter2)
                    {
                        continue;
                    }
                    poly2 = triangles[iter2];

                    for (i21 = 0; i21 < poly2.Count; i21++)
                    {
                        if ((d2.Position.X != poly2[i21].Position.X) || (d2.Position.Y != poly2[i21].Position.Y))
                        {
                            continue;
                        }
                        i22 = (i21 + 1) % poly2.Count;
                        if ((d1.Position.X != poly2[i22].Position.X) || (d1.Position.Y != poly2[i22].Position.Y))
                        {
                            continue;
                        }
                        isdiagonal = true;
                        break;
                    }
                    if (isdiagonal)
                    {
                        break;
                    }
                }

                if (!isdiagonal)
                {
                    continue;
                }

                p2 = poly1[i11].Position;
                i13 = i11 == 0 ? poly1.Count - 1 : i11 - 1;
                p1 = poly1[i13].Position;
                i23 = i22 == (poly2.Count - 1) ? 0 : i22 + 1;
                p3 = poly2[i23].Position;

                if (!IsConvex(p1, p2, p3))
                {
                    continue;
                }

                p2 = poly1[i12].Position;
                i13 = i12 == (poly1.Count - 1) ? 0 : i12 + 1;
                p3 = poly1[i13].Position;
                i23 = i21 == 0 ? poly2.Count - 1 : i21 - 1;
                p1 = poly2[i23].Position;

                if (!IsConvex(p1, p2, p3))
                {
                    continue;
                }

                var newpoly = new PolygonPoint[poly1.Count + poly2.Count - 2];
                k = 0;
                for (j = i12; j != i11; j = (j + 1) % poly1.Count)
                {
                    newpoly[k] = poly1[j];
                    k++;
                }
                for (j = i22; j != i21; j = (j + 1) % poly2.Count)
                {
                    newpoly[k] = poly2[j];
                    k++;
                }

                triangles.RemoveAt(iter2);
                poly1 = Polygon.Wrap(newpoly);
                triangles[iter1] = poly1;
                i11 = -1;
            }
        }

        parts.AddRange(triangles);

        return true;
    }

    private static bool ConvexPartitionHM(List<Polygon> inpolys, List<Polygon> parts)
    {
        var outpolys = new List<Polygon>();

        if (!RemoveHoles(inpolys, outpolys))
        {
            return false;
        }
        foreach (Polygon poly in outpolys)
        {
            if (!ConvexPartitionHM(poly, parts))
            {
                return false;
            }
        }
        return true;
    }

    // Dynamic programming state for minimum-weight triangulation.
    private struct DPState
    {
        public bool visible;
        public double weight;
        public int bestvertex;
    }

    // Minimum-weight polygon triangulation by dynamic programming.
    // Time complexity: O(n^3)
    // Space complexity: O(n^2)
    private static bool TriangulateOPT(Polygon poly, List<Polygon> triangles)
    {
        if (!poly.IsValid)
        {
            return false;
        }

        int i, j, k, gap, n;
        Vector2 p1, p2, p3, p4;
        int bestvertex;
        double weight, minweight = 0, d1, d2;
        Diagonal diagonal;
        var diagonals = new LinkedList<Diagonal>();
        bool ret = true;

        n = poly.Count;
        var dpstates = new DPState[n][];
        for (i = 1; i < n; i++)
        {
            dpstates[i] = new DPState[i];
        }

        // Initialize states and visibility.
        for (i = 0; i < (n - 1); i++)
        {
            p1 = poly[i].Position;
            for (j = i + 1; j < n; j++)
            {
                dpstates[j][i].visible = true;
                dpstates[j][i].weight = 0;
                dpstates[j][i].bestvertex = -1;
                if (j != (i + 1))
                {
                    p2 = poly[j].Position;

                    // Visibility check.
                    p3 = i == 0 ? poly[n - 1].Position : poly[i - 1].Position;
                    p4 = i == (n - 1) ? poly[0].Position : poly[i + 1].Position;
                    if (!InCone(p3, p1, p4, p2))
                    {
                        dpstates[j][i].visible = false;
                        continue;
                    }

                    p3 = j == 0 ? poly[n - 1].Position : poly[j - 1].Position;
                    p4 = j == (n - 1) ? poly[0].Position : poly[j + 1].Position;
                    if (!InCone(p3, p2, p4, p1))
                    {
                        dpstates[j][i].visible = false;
                        continue;
                    }

                    for (k = 0; k < n; k++)
                    {
                        p3 = poly[k].Position;
                        p4 = k == (n - 1) ? poly[0].Position : poly[k + 1].Position;
                        if (Intersects(p1, p2, p3, p4))
                        {
                            dpstates[j][i].visible = false;
                            break;
                        }
                    }
                }
            }
        }
        dpstates[n - 1][0].visible = true;
        dpstates[n - 1][0].weight = 0;
        dpstates[n - 1][0].bestvertex = -1;

        for (gap = 2; gap < n; gap++)
        {
            for (i = 0; i < (n - gap); i++)
            {
                j = i + gap;
                if (!dpstates[j][i].visible)
                {
                    continue;
                }
                bestvertex = -1;
                for (k = i + 1; k < j; k++)
                {
                    if (!dpstates[k][i].visible)
                    {
                        continue;
                    }
                    if (!dpstates[j][k].visible)
                    {
                        continue;
                    }

                    d1 = k <= (i + 1) ? 0 : Distance(poly[i].Position, poly[k].Position);
                    d2 = j <= (k + 1) ? 0 : Distance(poly[k].Position, poly[j].Position);

                    weight = dpstates[k][i].weight + dpstates[j][k].weight + d1 + d2;

                    if ((bestvertex == -1) || (weight < minweight))
                    {
                        bestvertex = k;
                        minweight = weight;
                    }
                }
                if (bestvertex == -1)
                {
                    return false;
                }

                dpstates[j][i].bestvertex = bestvertex;
                dpstates[j][i].weight = minweight;
            }
        }

        diagonals.AddLast(new Diagonal(0, n - 1));
        while (diagonals.Count > 0)
        {
            diagonal = diagonals.First!.Value;
            diagonals.RemoveFirst();
            bestvertex = dpstates[diagonal.index2][diagonal.index1].bestvertex;
            if (bestvertex == -1)
            {
                ret = false;
                break;
            }
            triangles.Add(Polygon.Triangle(poly[diagonal.index1], poly[bestvertex], poly[diagonal.index2]));
            if (bestvertex > (diagonal.index1 + 1))
            {
                diagonals.AddLast(new Diagonal(diagonal.index1, bestvertex));
            }
            if (diagonal.index2 > (bestvertex + 1))
            {
                diagonals.AddLast(new Diagonal(bestvertex, diagonal.index2));
            }
        }

        return ret;
    }
}
