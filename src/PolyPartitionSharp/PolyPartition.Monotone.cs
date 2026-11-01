using System.Numerics;

namespace PolyPartitionSharp;

public static partial class PolyPartition
{
    private enum VertexType
    {
        Regular = 0,
        Start = 1,
        End = 2,
        Split = 3,
        Merge = 4,
    }

    private struct MonotoneVertex
    {
        public PolygonPoint p;
        public int previous;
        public int next;
    }

    // Edge that intersects the scanline.
    private sealed class ScanLineEdge
    {
        public int index;
        public Vector2 p1;
        public Vector2 p2;

        // Determines if the edge is to the left of another edge.
        public bool IsLessThan(ScanLineEdge other)
        {
            if (other.p1.Y == other.p2.Y)
            {
                if (p1.Y == p2.Y)
                {
                    return p1.Y < other.p1.Y;
                }
                return IsConvex(p1, p2, other.p1);
            }
            else if (p1.Y == p2.Y)
            {
                return !IsConvex(other.p1, other.p2, p1);
            }
            else if (p1.Y < other.p1.Y)
            {
                return !IsConvex(other.p1, other.p2, p1);
            }
            else
            {
                return IsConvex(p1, p2, other.p1);
            }
        }
    }

    // Stands in for upstream's std::set<ScanLineEdge>: a list kept sorted by IsLessThan.
    // shortcut: insert/erase are O(n) array shifts instead of O(log n), switch to a balanced tree if huge inputs get slow.
    private sealed class EdgeTree
    {
        private readonly List<ScanLineEdge> _edges = [];

        public ScanLineEdge this[int index] => _edges[index];

        // Index of the first edge that is not less than the probe.
        public int LowerBound(ScanLineEdge probe)
        {
            int lo = 0, hi = _edges.Count;
            while (lo < hi)
            {
                int mid = lo + ((hi - lo) / 2);
                if (_edges[mid].IsLessThan(probe))
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }
            return lo;
        }

        // Like std::set::insert: returns the existing equivalent edge instead of inserting a duplicate.
        public ScanLineEdge Insert(ScanLineEdge edge)
        {
            int pos = LowerBound(edge);
            if (pos < _edges.Count && !edge.IsLessThan(_edges[pos]))
            {
                return _edges[pos];
            }
            _edges.Insert(pos, edge);
            return edge;
        }

        public void Erase(ScanLineEdge? edge)
        {
            // Upstream erases end() here, which is undefined behavior; do nothing instead.
            if (edge is null)
            {
                return;
            }
            int pos = LowerBound(edge);
            if (pos >= _edges.Count || _edges[pos] != edge)
            {
                pos = _edges.IndexOf(edge);
            }
            if (pos >= 0)
            {
                _edges.RemoveAt(pos);
            }
        }
    }

    // Creates a monotone partition of a list of polygons that
    // can contain holes. Triangulates a set of polygons by
    // first partitioning them into monotone polygons.
    // Time complexity: O(n*log(n)), n is the number of vertices.
    // Space complexity: O(n)
    // The algorithm used here is outlined in the book
    // "Computational Geometry: Algorithms and Applications"
    // by Mark de Berg, Otfried Cheong, Marc van Kreveld, and Mark Overmars.
    private static bool MonotonePartition(List<Polygon> inpolys, List<Polygon> monotonePolys)
    {
        int i, numvertices, vindex, vindex2, newnumvertices, maxnumvertices;
        int polystartindex, polyendindex;
        ScanLineEdge edgeIter;
        bool error = false;

        numvertices = 0;
        foreach (Polygon poly in inpolys)
        {
            if (!poly.IsValid)
            {
                return false;
            }
            numvertices += poly.Count;
        }

        maxnumvertices = numvertices * 3;
        var vertices = new MonotoneVertex[maxnumvertices];
        newnumvertices = numvertices;

        polystartindex = 0;
        foreach (Polygon poly in inpolys)
        {
            polyendindex = polystartindex + poly.Count - 1;
            for (i = 0; i < poly.Count; i++)
            {
                vertices[i + polystartindex].p = poly[i];
                vertices[i + polystartindex].previous = i == 0 ? polyendindex : i + polystartindex - 1;
                vertices[i + polystartindex].next = i == (poly.Count - 1) ? polystartindex : i + polystartindex + 1;
            }
            polystartindex = polyendindex + 1;
        }

        // Construct the priority queue.
        var priority = new int[numvertices];
        for (i = 0; i < numvertices; i++)
        {
            priority[i] = i;
        }
        Array.Sort(priority, (index1, index2) => VertexSorter(vertices, index1, index2) ? -1 : VertexSorter(vertices, index2, index1) ? 1 : 0);

        // Determine vertex types.
        var vertextypes = new VertexType[maxnumvertices];
        for (i = 0; i < numvertices; i++)
        {
            Vector2 vp = vertices[i].p.Position;
            Vector2 vprev = vertices[vertices[i].previous].p.Position;
            Vector2 vnext = vertices[vertices[i].next].p.Position;

            if (Below(vprev, vp) && Below(vnext, vp))
            {
                vertextypes[i] = IsConvex(vnext, vprev, vp) ? VertexType.Start : VertexType.Split;
            }
            else if (Below(vp, vprev) && Below(vp, vnext))
            {
                vertextypes[i] = IsConvex(vnext, vprev, vp) ? VertexType.End : VertexType.Merge;
            }
            else
            {
                vertextypes[i] = VertexType.Regular;
            }
        }

        // Helpers.
        var helpers = new int[maxnumvertices];

        // Binary search tree that holds edges intersecting the scanline.
        var edgeTree = new EdgeTree();
        // Store references to the edge tree elements (null stands for end()).
        // This makes deleting existing edges much faster.
        var edgeTreeIterators = new ScanLineEdge?[maxnumvertices];

        // For each vertex.
        for (i = 0; i < numvertices; i++)
        {
            vindex = priority[i];
            vindex2 = vindex;
            int vprevious = vertices[vindex].previous;

            // Depending on the vertex type, do the appropriate action.
            // Comments in the following sections are copied from
            // "Computational Geometry: Algorithms and Applications".
            // Notation: e_i = e subscript i, v_i = v subscript i, etc.
            switch (vertextypes[vindex])
            {
                case VertexType.Start:
                    // Insert e_i in T and set helper(e_i) to v_i.
                    edgeTreeIterators[vindex] = edgeTree.Insert(new ScanLineEdge
                    {
                        p1 = vertices[vindex].p.Position,
                        p2 = vertices[vertices[vindex].next].p.Position,
                        index = vindex,
                    });
                    helpers[vindex] = vindex;
                    break;

                case VertexType.End:
                    if (edgeTreeIterators[vprevious] is null)
                    {
                        error = true;
                        break;
                    }
                    // If helper(e_i - 1) is a merge vertex
                    if (vertextypes[helpers[vprevious]] == VertexType.Merge)
                    {
                        // Insert the diagonal connecting vi to helper(e_i - 1) in D.
                        AddDiagonal(vertices, ref newnumvertices, vindex, helpers[vprevious],
                                vertextypes, edgeTreeIterators, helpers);
                    }
                    // Delete e_i - 1 from T
                    edgeTree.Erase(edgeTreeIterators[vertices[vindex].previous]);
                    break;

                case VertexType.Split:
                    {
                        // Search in T to find the edge e_j directly left of v_i.
                        int pos = edgeTree.LowerBound(new ScanLineEdge { p1 = vertices[vindex].p.Position, p2 = vertices[vindex].p.Position });
                        if (pos == 0)
                        {
                            error = true;
                            break;
                        }
                        edgeIter = edgeTree[pos - 1];
                        // Insert the diagonal connecting vi to helper(e_j) in D.
                        AddDiagonal(vertices, ref newnumvertices, vindex, helpers[edgeIter.index],
                                vertextypes, edgeTreeIterators, helpers);
                        vindex2 = newnumvertices - 2;
                        // helper(e_j) in v_i.
                        helpers[edgeIter.index] = vindex;
                        // Insert e_i in T and set helper(e_i) to v_i.
                        edgeTreeIterators[vindex2] = edgeTree.Insert(new ScanLineEdge
                        {
                            p1 = vertices[vindex2].p.Position,
                            p2 = vertices[vertices[vindex2].next].p.Position,
                            index = vindex2,
                        });
                        helpers[vindex2] = vindex2;
                        break;
                    }

                case VertexType.Merge:
                    {
                        if (edgeTreeIterators[vprevious] is null)
                        {
                            error = true;
                            break;
                        }
                        // if helper(e_i - 1) is a merge vertex
                        if (vertextypes[helpers[vprevious]] == VertexType.Merge)
                        {
                            // Insert the diagonal connecting vi to helper(e_i - 1) in D.
                            AddDiagonal(vertices, ref newnumvertices, vindex, helpers[vprevious],
                                    vertextypes, edgeTreeIterators, helpers);
                            vindex2 = newnumvertices - 2;
                        }
                        // Delete e_i - 1 from T.
                        edgeTree.Erase(edgeTreeIterators[vertices[vindex].previous]);
                        // Search in T to find the edge e_j directly left of v_i.
                        int pos = edgeTree.LowerBound(new ScanLineEdge { p1 = vertices[vindex].p.Position, p2 = vertices[vindex].p.Position });
                        if (pos == 0)
                        {
                            error = true;
                            break;
                        }
                        edgeIter = edgeTree[pos - 1];
                        // If helper(e_j) is a merge vertex.
                        if (vertextypes[helpers[edgeIter.index]] == VertexType.Merge)
                        {
                            // Insert the diagonal connecting v_i to helper(e_j) in D.
                            AddDiagonal(vertices, ref newnumvertices, vindex2, helpers[edgeIter.index],
                                    vertextypes, edgeTreeIterators, helpers);
                        }
                        // helper(e_j) <- v_i
                        helpers[edgeIter.index] = vindex2;
                        break;
                    }

                case VertexType.Regular:
                    // If the interior of P lies to the right of v_i.
                    if (Below(vertices[vindex].p.Position, vertices[vprevious].p.Position))
                    {
                        if (edgeTreeIterators[vprevious] is null)
                        {
                            error = true;
                            break;
                        }
                        // If helper(e_i - 1) is a merge vertex.
                        if (vertextypes[helpers[vprevious]] == VertexType.Merge)
                        {
                            // Insert the diagonal connecting v_i to helper(e_i - 1) in D.
                            AddDiagonal(vertices, ref newnumvertices, vindex, helpers[vprevious],
                                    vertextypes, edgeTreeIterators, helpers);
                            vindex2 = newnumvertices - 2;
                        }
                        // Delete e_i - 1 from T.
                        edgeTree.Erase(edgeTreeIterators[vertices[vindex].previous]);
                        // Insert e_i in T and set helper(e_i) to v_i.
                        edgeTreeIterators[vindex2] = edgeTree.Insert(new ScanLineEdge
                        {
                            p1 = vertices[vindex2].p.Position,
                            p2 = vertices[vertices[vindex2].next].p.Position,
                            index = vindex2,
                        });
                        helpers[vindex2] = vindex;
                    }
                    else
                    {
                        // Search in T to find the edge e_j directly left of v_i.
                        int pos = edgeTree.LowerBound(new ScanLineEdge { p1 = vertices[vindex].p.Position, p2 = vertices[vindex].p.Position });
                        if (pos == 0)
                        {
                            error = true;
                            break;
                        }
                        edgeIter = edgeTree[pos - 1];
                        // If helper(e_j) is a merge vertex.
                        if (vertextypes[helpers[edgeIter.index]] == VertexType.Merge)
                        {
                            // Insert the diagonal connecting v_i to helper(e_j) in D.
                            AddDiagonal(vertices, ref newnumvertices, vindex, helpers[edgeIter.index],
                                    vertextypes, edgeTreeIterators, helpers);
                        }
                        // helper(e_j) <- v_i.
                        helpers[edgeIter.index] = vindex;
                    }
                    break;
            }

            if (error)
            {
                break;
            }
        }

        if (error)
        {
            return false;
        }

        // Return result.
        var used = new bool[newnumvertices];
        for (i = 0; i < newnumvertices; i++)
        {
            if (used[i])
            {
                continue;
            }
            int v = i;
            int vnext = vertices[v].next;
            int size = 1;
            while (vnext != v)
            {
                vnext = vertices[vnext].next;
                size++;
            }
            var mpoly = new PolygonPoint[size];
            mpoly[0] = vertices[v].p;
            vnext = vertices[v].next;
            size = 1;
            used[i] = true;
            used[vertices[v].next] = true;
            while (vnext != v)
            {
                mpoly[size] = vertices[vnext].p;
                used[vertices[vnext].next] = true;
                vnext = vertices[vnext].next;
                size++;
            }
            monotonePolys.Add(Polygon.Wrap(mpoly));
        }

        return true;
    }

    // Adds a diagonal to the doubly-connected list of vertices.
    private static void AddDiagonal(MonotoneVertex[] vertices, ref int numvertices, int index1, int index2,
            VertexType[] vertextypes, ScanLineEdge?[] edgeTreeIterators, int[] helpers)
    {
        int newindex1, newindex2;

        newindex1 = numvertices;
        numvertices++;
        newindex2 = numvertices;
        numvertices++;

        vertices[newindex1].p = vertices[index1].p;
        vertices[newindex2].p = vertices[index2].p;

        vertices[newindex2].next = vertices[index2].next;
        vertices[newindex1].next = vertices[index1].next;

        vertices[vertices[index2].next].previous = newindex2;
        vertices[vertices[index1].next].previous = newindex1;

        vertices[index1].next = newindex2;
        vertices[newindex2].previous = index1;

        vertices[index2].next = newindex1;
        vertices[newindex1].previous = index2;

        // Update all relevant structures.
        vertextypes[newindex1] = vertextypes[index1];
        edgeTreeIterators[newindex1] = edgeTreeIterators[index1];
        helpers[newindex1] = helpers[index1];
        if (edgeTreeIterators[newindex1] is { } edge1)
        {
            edge1.index = newindex1;
        }
        vertextypes[newindex2] = vertextypes[index2];
        edgeTreeIterators[newindex2] = edgeTreeIterators[index2];
        helpers[newindex2] = helpers[index2];
        if (edgeTreeIterators[newindex2] is { } edge2)
        {
            edge2.index = newindex2;
        }
    }

    private static bool Below(Vector2 p1, Vector2 p2)
    {
        if (p1.Y < p2.Y)
        {
            return true;
        }
        else if (p1.Y == p2.Y)
        {
            if (p1.X < p2.X)
            {
                return true;
            }
        }
        return false;
    }

    // Sorts in the falling order of y values, if y is equal, x is used instead.
    private static bool VertexSorter(MonotoneVertex[] vertices, int index1, int index2)
    {
        if (vertices[index1].p.Position.Y > vertices[index2].p.Position.Y)
        {
            return true;
        }
        else if (vertices[index1].p.Position.Y == vertices[index2].p.Position.Y)
        {
            if (vertices[index1].p.Position.X > vertices[index2].p.Position.X)
            {
                return true;
            }
        }
        return false;
    }

    // Triangulates monotone polygon.
    // Time complexity: O(n)
    // Space complexity: O(n)
    private static bool TriangulateMonotone(Polygon inPoly, List<Polygon> triangles)
    {
        if (!inPoly.IsValid)
        {
            return false;
        }

        int i, i2, j, topindex, bottomindex, leftindex, rightindex, vindex;
        PolygonPoint[] points = inPoly.Points;
        int numpoints = inPoly.Count;

        // Trivial case.
        if (numpoints == 3)
        {
            triangles.Add(inPoly);
            return true;
        }

        topindex = 0;
        bottomindex = 0;
        for (i = 1; i < numpoints; i++)
        {
            if (Below(points[i].Position, points[bottomindex].Position))
            {
                bottomindex = i;
            }
            if (Below(points[topindex].Position, points[i].Position))
            {
                topindex = i;
            }
        }

        // Check if the poly is really monotone.
        i = topindex;
        while (i != bottomindex)
        {
            i2 = i + 1;
            if (i2 >= numpoints)
            {
                i2 = 0;
            }
            if (!Below(points[i2].Position, points[i].Position))
            {
                return false;
            }
            i = i2;
        }
        i = bottomindex;
        while (i != topindex)
        {
            i2 = i + 1;
            if (i2 >= numpoints)
            {
                i2 = 0;
            }
            if (!Below(points[i].Position, points[i2].Position))
            {
                return false;
            }
            i = i2;
        }

        var vertextypes = new sbyte[numpoints];
        var priority = new int[numpoints];

        // Merge left and right vertex chains.
        priority[0] = topindex;
        vertextypes[topindex] = 0;
        leftindex = topindex + 1;
        if (leftindex >= numpoints)
        {
            leftindex = 0;
        }
        rightindex = topindex - 1;
        if (rightindex < 0)
        {
            rightindex = numpoints - 1;
        }
        for (i = 1; i < (numpoints - 1); i++)
        {
            if (leftindex == bottomindex)
            {
                priority[i] = rightindex;
                rightindex--;
                if (rightindex < 0)
                {
                    rightindex = numpoints - 1;
                }
                vertextypes[priority[i]] = -1;
            }
            else if (rightindex == bottomindex)
            {
                priority[i] = leftindex;
                leftindex++;
                if (leftindex >= numpoints)
                {
                    leftindex = 0;
                }
                vertextypes[priority[i]] = 1;
            }
            else
            {
                if (Below(points[leftindex].Position, points[rightindex].Position))
                {
                    priority[i] = rightindex;
                    rightindex--;
                    if (rightindex < 0)
                    {
                        rightindex = numpoints - 1;
                    }
                    vertextypes[priority[i]] = -1;
                }
                else
                {
                    priority[i] = leftindex;
                    leftindex++;
                    if (leftindex >= numpoints)
                    {
                        leftindex = 0;
                    }
                    vertextypes[priority[i]] = 1;
                }
            }
        }
        priority[i] = bottomindex;
        vertextypes[bottomindex] = 0;

        var stack = new int[numpoints];
        int stackptr;

        stack[0] = priority[0];
        stack[1] = priority[1];
        stackptr = 2;

        // For each vertex from top to bottom trim as many triangles as possible.
        for (i = 2; i < (numpoints - 1); i++)
        {
            vindex = priority[i];
            if (vertextypes[vindex] != vertextypes[stack[stackptr - 1]])
            {
                for (j = 0; j < (stackptr - 1); j++)
                {
                    if (vertextypes[vindex] == 1)
                    {
                        triangles.Add(Polygon.Triangle(points[stack[j + 1]], points[stack[j]], points[vindex]));
                    }
                    else
                    {
                        triangles.Add(Polygon.Triangle(points[stack[j]], points[stack[j + 1]], points[vindex]));
                    }
                }
                stack[0] = priority[i - 1];
                stack[1] = priority[i];
                stackptr = 2;
            }
            else
            {
                stackptr--;
                while (stackptr > 0)
                {
                    if (vertextypes[vindex] == 1)
                    {
                        if (IsConvex(points[vindex].Position, points[stack[stackptr - 1]].Position, points[stack[stackptr]].Position))
                        {
                            triangles.Add(Polygon.Triangle(points[vindex], points[stack[stackptr - 1]], points[stack[stackptr]]));
                            stackptr--;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (IsConvex(points[vindex].Position, points[stack[stackptr]].Position, points[stack[stackptr - 1]].Position))
                        {
                            triangles.Add(Polygon.Triangle(points[vindex], points[stack[stackptr]], points[stack[stackptr - 1]]));
                            stackptr--;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                stackptr++;
                stack[stackptr] = vindex;
                stackptr++;
            }
        }
        vindex = priority[i];
        for (j = 0; j < (stackptr - 1); j++)
        {
            if (vertextypes[stack[j + 1]] == 1)
            {
                triangles.Add(Polygon.Triangle(points[stack[j]], points[stack[j + 1]], points[vindex]));
            }
            else
            {
                triangles.Add(Polygon.Triangle(points[stack[j + 1]], points[stack[j]], points[vindex]));
            }
        }

        return true;
    }

    private static bool TriangulateMONO(List<Polygon> inpolys, List<Polygon> triangles)
    {
        var monotone = new List<Polygon>();

        if (!MonotonePartition(inpolys, monotone))
        {
            return false;
        }
        foreach (Polygon poly in monotone)
        {
            if (!TriangulateMonotone(poly, triangles))
            {
                return false;
            }
        }
        return true;
    }
}
