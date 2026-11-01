using System.Numerics;

namespace PolyPartitionSharp;

public static partial class PolyPartition
{
    // Dynamic programming state for convex partitioning.
    private struct DPState2
    {
        public bool visible;
        public long weight;
        public LinkedList<Diagonal> pairs;
    }

    private static void UpdateState(int a, int b, long w, int i, int j, DPState2[][] dpstates)
    {
        long w2 = dpstates[a][b].weight;
        if (w > w2)
        {
            return;
        }

        LinkedList<Diagonal> pairs = dpstates[a][b].pairs;
        var newdiagonal = new Diagonal(i, j);

        if (w < w2)
        {
            pairs.Clear();
            pairs.AddFirst(newdiagonal);
            dpstates[a][b].weight = w;
        }
        else
        {
            if ((pairs.Count > 0) && (i <= pairs.First!.Value.index1))
            {
                return;
            }
            while ((pairs.Count > 0) && (pairs.First!.Value.index2 >= j))
            {
                pairs.RemoveFirst();
            }
            pairs.AddFirst(newdiagonal);
        }
    }

    private static void TypeA(int i, int j, int k, PartitionVertex[] vertices, DPState2[][] dpstates)
    {
        int top;
        long w;

        if (!dpstates[i][j].visible)
        {
            return;
        }
        top = j;
        w = dpstates[i][j].weight;
        if (k - j > 1)
        {
            if (!dpstates[j][k].visible)
            {
                return;
            }
            w += dpstates[j][k].weight + 1;
        }
        if (j - i > 1)
        {
            LinkedList<Diagonal> pairs = dpstates[i][j].pairs;
            LinkedListNode<Diagonal>? lastiter = null;
            for (LinkedListNode<Diagonal>? iter = pairs.Last; iter is not null; iter = iter.Previous)
            {
                if (!IsReflex(vertices[iter.Value.index2].p.Position, vertices[j].p.Position, vertices[k].p.Position))
                {
                    lastiter = iter;
                }
                else
                {
                    break;
                }
            }
            if (lastiter is null)
            {
                w++;
            }
            else
            {
                if (IsReflex(vertices[k].p.Position, vertices[i].p.Position, vertices[lastiter.Value.index1].p.Position))
                {
                    w++;
                }
                else
                {
                    top = lastiter.Value.index1;
                }
            }
        }
        UpdateState(i, k, w, top, j, dpstates);
    }

    private static void TypeB(int i, int j, int k, PartitionVertex[] vertices, DPState2[][] dpstates)
    {
        int top;
        long w;

        if (!dpstates[j][k].visible)
        {
            return;
        }
        top = j;
        w = dpstates[j][k].weight;

        if (j - i > 1)
        {
            if (!dpstates[i][j].visible)
            {
                return;
            }
            w += dpstates[i][j].weight + 1;
        }
        if (k - j > 1)
        {
            LinkedList<Diagonal> pairs = dpstates[j][k].pairs;

            LinkedListNode<Diagonal>? iter = pairs.First;
            if ((iter is not null) && (!IsReflex(vertices[i].p.Position, vertices[j].p.Position, vertices[iter.Value.index1].p.Position)))
            {
                LinkedListNode<Diagonal> lastiter = iter;
                while (iter is not null)
                {
                    if (!IsReflex(vertices[i].p.Position, vertices[j].p.Position, vertices[iter.Value.index1].p.Position))
                    {
                        lastiter = iter;
                        iter = iter.Next;
                    }
                    else
                    {
                        break;
                    }
                }
                if (IsReflex(vertices[lastiter.Value.index2].p.Position, vertices[k].p.Position, vertices[i].p.Position))
                {
                    w++;
                }
                else
                {
                    top = lastiter.Value.index2;
                }
            }
            else
            {
                w++;
            }
        }
        UpdateState(i, k, w, j, top, dpstates);
    }

    private static bool ConvexPartitionOPT(Polygon poly, List<Polygon> parts)
    {
        if (!poly.IsValid)
        {
            return false;
        }

        Vector2 p1, p2, p3, p4;
        int i, j, k, n, gap;
        var diagonals = new LinkedList<Diagonal>();
        var diagonals2 = new LinkedList<Diagonal>();
        Diagonal diagonal;
        LinkedList<Diagonal> pairs, pairs2;
        LinkedListNode<Diagonal> iter, iter2;
        bool ret;
        var indices = new List<int>();
        bool ijreal, jkreal;

        n = poly.Count;
        var vertices = new PartitionVertex[n];

        var dpstates = new DPState2[n][];
        for (i = 0; i < n; i++)
        {
            dpstates[i] = new DPState2[n];
            for (j = 0; j < n; j++)
            {
                dpstates[i][j].pairs = new LinkedList<Diagonal>();
            }
        }

        // Initialize vertex information.
        for (i = 0; i < n; i++)
        {
            vertices[i] = new PartitionVertex();
        }
        for (i = 0; i < n; i++)
        {
            vertices[i].p = poly[i];
            vertices[i].isActive = true;
            vertices[i].previous = i == 0 ? vertices[n - 1] : vertices[i - 1];
            vertices[i].next = i == (poly.Count - 1) ? vertices[0] : vertices[i + 1];
        }
        for (i = 1; i < n; i++)
        {
            UpdateVertexReflexity(vertices[i]);
        }

        // Initialize states and visibility.
        for (i = 0; i < (n - 1); i++)
        {
            p1 = poly[i].Position;
            for (j = i + 1; j < n; j++)
            {
                dpstates[i][j].visible = true;
                dpstates[i][j].weight = j == i + 1 ? 0 : 2147483647;
                if (j != (i + 1))
                {
                    p2 = poly[j].Position;

                    // Visibility check.
                    if (!InCone(vertices[i], p2))
                    {
                        dpstates[i][j].visible = false;
                        continue;
                    }
                    if (!InCone(vertices[j], p1))
                    {
                        dpstates[i][j].visible = false;
                        continue;
                    }

                    for (k = 0; k < n; k++)
                    {
                        p3 = poly[k].Position;
                        p4 = k == (n - 1) ? poly[0].Position : poly[k + 1].Position;
                        if (Intersects(p1, p2, p3, p4))
                        {
                            dpstates[i][j].visible = false;
                            break;
                        }
                    }
                }
            }
        }
        for (i = 0; i < (n - 2); i++)
        {
            j = i + 2;
            if (dpstates[i][j].visible)
            {
                dpstates[i][j].weight = 0;
                dpstates[i][j].pairs.AddLast(new Diagonal(i + 1, i + 1));
            }
        }

        dpstates[0][n - 1].visible = true;
        vertices[0].isConvex = false; // By convention.

        for (gap = 3; gap < n; gap++)
        {
            for (i = 0; i < n - gap; i++)
            {
                if (vertices[i].isConvex)
                {
                    continue;
                }
                k = i + gap;
                if (dpstates[i][k].visible)
                {
                    if (!vertices[k].isConvex)
                    {
                        for (j = i + 1; j < k; j++)
                        {
                            TypeA(i, j, k, vertices, dpstates);
                        }
                    }
                    else
                    {
                        for (j = i + 1; j < (k - 1); j++)
                        {
                            if (vertices[j].isConvex)
                            {
                                continue;
                            }
                            TypeA(i, j, k, vertices, dpstates);
                        }
                        TypeA(i, k - 1, k, vertices, dpstates);
                    }
                }
            }
            for (k = gap; k < n; k++)
            {
                if (vertices[k].isConvex)
                {
                    continue;
                }
                i = k - gap;
                if (vertices[i].isConvex && dpstates[i][k].visible)
                {
                    TypeB(i, i + 1, k, vertices, dpstates);
                    for (j = i + 2; j < k; j++)
                    {
                        if (vertices[j].isConvex)
                        {
                            continue;
                        }
                        TypeB(i, j, k, vertices, dpstates);
                    }
                }
            }
        }

        // Recover solution.
        ret = true;
        diagonals.AddFirst(new Diagonal(0, n - 1));
        while (diagonals.Count > 0)
        {
            diagonal = diagonals.First!.Value;
            diagonals.RemoveFirst();
            if ((diagonal.index2 - diagonal.index1) <= 1)
            {
                continue;
            }
            pairs = dpstates[diagonal.index1][diagonal.index2].pairs;
            if (pairs.Count == 0)
            {
                ret = false;
                break;
            }
            if (!vertices[diagonal.index1].isConvex)
            {
                iter = pairs.Last!;
                j = iter.Value.index2;
                diagonals.AddFirst(new Diagonal(j, diagonal.index2));
                if ((j - diagonal.index1) > 1)
                {
                    if (iter.Value.index1 != iter.Value.index2)
                    {
                        pairs2 = dpstates[diagonal.index1][j].pairs;
                        while (true)
                        {
                            if (pairs2.Count == 0)
                            {
                                ret = false;
                                break;
                            }
                            iter2 = pairs2.Last!;
                            if (iter.Value.index1 != iter2.Value.index1)
                            {
                                pairs2.RemoveLast();
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (!ret)
                        {
                            break;
                        }
                    }
                    diagonals.AddFirst(new Diagonal(diagonal.index1, j));
                }
            }
            else
            {
                iter = pairs.First!;
                j = iter.Value.index1;
                diagonals.AddFirst(new Diagonal(diagonal.index1, j));
                if ((diagonal.index2 - j) > 1)
                {
                    if (iter.Value.index1 != iter.Value.index2)
                    {
                        pairs2 = dpstates[j][diagonal.index2].pairs;
                        while (true)
                        {
                            if (pairs2.Count == 0)
                            {
                                ret = false;
                                break;
                            }
                            iter2 = pairs2.First!;
                            if (iter.Value.index2 != iter2.Value.index2)
                            {
                                pairs2.RemoveFirst();
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (!ret)
                        {
                            break;
                        }
                    }
                    diagonals.AddFirst(new Diagonal(j, diagonal.index2));
                }
            }
        }

        if (!ret)
        {
            return ret;
        }

        diagonals.AddFirst(new Diagonal(0, n - 1));
        while (diagonals.Count > 0)
        {
            diagonal = diagonals.First!.Value;
            diagonals.RemoveFirst();
            if ((diagonal.index2 - diagonal.index1) <= 1)
            {
                continue;
            }

            indices.Clear();
            diagonals2.Clear();
            indices.Add(diagonal.index1);
            indices.Add(diagonal.index2);
            diagonals2.AddFirst(diagonal);

            while (diagonals2.Count > 0)
            {
                diagonal = diagonals2.First!.Value;
                diagonals2.RemoveFirst();
                if ((diagonal.index2 - diagonal.index1) <= 1)
                {
                    continue;
                }
                ijreal = true;
                jkreal = true;
                pairs = dpstates[diagonal.index1][diagonal.index2].pairs;
                if (!vertices[diagonal.index1].isConvex)
                {
                    iter = pairs.Last!;
                    j = iter.Value.index2;
                    if (iter.Value.index1 != iter.Value.index2)
                    {
                        ijreal = false;
                    }
                }
                else
                {
                    iter = pairs.First!;
                    j = iter.Value.index1;
                    if (iter.Value.index1 != iter.Value.index2)
                    {
                        jkreal = false;
                    }
                }

                if (ijreal)
                {
                    diagonals.AddLast(new Diagonal(diagonal.index1, j));
                }
                else
                {
                    diagonals2.AddLast(new Diagonal(diagonal.index1, j));
                }

                if (jkreal)
                {
                    diagonals.AddLast(new Diagonal(j, diagonal.index2));
                }
                else
                {
                    diagonals2.AddLast(new Diagonal(j, diagonal.index2));
                }

                indices.Add(j);
            }

            indices.Sort();
            var newpoly = new PolygonPoint[indices.Count];
            for (k = 0; k < indices.Count; k++)
            {
                newpoly[k] = vertices[indices[k]].p;
            }
            parts.Add(Polygon.Wrap(newpoly));
        }

        return ret;
    }
}
