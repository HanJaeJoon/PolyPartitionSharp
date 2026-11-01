## PolyPartitionSharp

[![Build](https://github.com/HanJaeJoon/PolyPartitionSharp/actions/workflows/nuget-publish.yml/badge.svg)](https://github.com/HanJaeJoon/PolyPartitionSharp/actions/workflows/nuget-publish.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

PolyPartitionSharp is a pure C# port of [polypartition](https://github.com/ivanfratric/polypartition)
by Ivan Fratric: polygon triangulation and convex partitioning for 2D polygons, with or without holes.
The algorithms are ported line for line, so the results match the upstream library
(the upstream test fixtures are part of the test suite).

Targets `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.

## Installation

``` bash
dotnet add package PolyPartitionSharp
```

## Usage

``` c#
using System.Numerics;
using PolyPartitionSharp;

// Non-hole polygons must be counter-clockwise, holes clockwise.
var outer = new Polygon([new Vector2(0, 0), new Vector2(4, 0), new Vector2(4, 4), new Vector2(0, 4)]);
var hole = new Polygon([new Vector2(1, 1), new Vector2(1, 3), new Vector2(3, 3), new Vector2(3, 1)], isHole: true);

if (PolyPartition.TryTriangulateEC([outer, hole], out List<Polygon> triangles))
{
    foreach (Polygon triangle in triangles)
    {
        Console.WriteLine(string.Join(", ", triangle.Select(p => p.Position)));
    }
}
```

Use `Polygon.SetOrientation(PolygonOrientation.CounterClockwise)` to fix the winding of an input.
Each `PolygonPoint` carries a user `Id` that is copied into the output, so output vertices can be
mapped back to the input.

## Algorithms

| Method | Upstream | Holes | Time | Notes |
|---|---|---|---|---|
| `TryTriangulateEC` | `Triangulate_EC` | yes (list overload) | O(n^2) | Ear clipping |
| `TryTriangulateOPT` | `Triangulate_OPT` | no | O(n^3) | Minimal total edge length |
| `TryTriangulateMONO` | `Triangulate_MONO` | yes (list overload) | O(n log n) | Monotone partition, then triangulation |
| `TryMonotonePartition` | `MonotonePartition` | yes | O(n log n) | Monotone pieces |
| `TryConvexPartitionHM` | `ConvexPartition_HM` | yes (list overload) | O(n^2) | Hertel-Mehlhorn, at most 4x optimal |
| `TryConvexPartitionOPT` | `ConvexPartition_OPT` | no | O(n^3) | Optimal number of convex parts (Keil-Snoeyink); rejects bare triangles, as upstream |
| `TryRemoveHoles` | `RemoveHoles` | yes | O(h n^2) | Merges holes into their outer polygon |

Every method returns `false` where upstream returns 0. Vertex positions are `System.Numerics.Vector2`;
all geometry is computed in double precision, as upstream does.

## License

MIT. The original polypartition is Copyright (c) 2011-2021 Ivan Fratric and contributors, under the
MIT license; see [LICENSE](LICENSE).
