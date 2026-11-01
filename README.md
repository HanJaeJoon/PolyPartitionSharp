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

### Index buffer

Use vertex buffer indices as point ids, then read the ids back from the triangles. The `Vector2`
constructor gives every point id 0, so build the polygons from `PolygonPoint`s instead.

``` c#
Vector2[] vertices = [new(0, 0), new(4, 0), new(4, 4), new(0, 4), new(1, 1), new(1, 3), new(3, 3), new(3, 1)];
var outer = new Polygon(Enumerable.Range(0, 4).Select(i => new PolygonPoint(vertices[i], i)));
var hole = new Polygon(Enumerable.Range(4, 4).Select(i => new PolygonPoint(vertices[i], i)), isHole: true);

if (PolyPartition.TryTriangulateEC([outer, hole], out List<Polygon> triangles))
{
    int[] indices = triangles.SelectMany(t => t.Select(p => p.Id)).ToArray();
}
```

## Input requirements

- Every polygon is simple: no self-intersections, no zero-width spikes.
- No repeated closing vertex (GeoJSON and SVG repeat the first point at the end; drop it).
- No consecutive duplicate vertices.
- Finite coordinates (no NaN or Infinity).
- Non-hole polygons counter-clockwise, holes clockwise, and every hole strictly inside its outer
  polygon (not touching its edges or vertices).

Other input is not rejected up front. Depending on the algorithm it returns `false`, or `true` with
zero-area or clockwise triangles, non-convex parts, or NaN areas; see the
[per-algorithm table](https://github.com/HanJaeJoon/PolyPartitionSharp/blob/main/docs/dogfooding/round1.md#degenerate-input-that-is-out-of-contract-but-common).

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

With many holes, `TryTriangulateMONO` is much faster than `TryTriangulateEC` (about 160x at 400 holes,
since EC and HM remove holes in O(h n^2)), but see limitation B below.

## Known limitations (inherited from upstream)

These are upstream behaviors, kept so results stay identical to polypartition. In each case the method
returns `true` with wrong output:

- **A. `TryConvexPartitionOPT` can return non-convex parts** (about 5% of random simple polygons;
  areas still add up). Workaround: check every part for convexity before using it, e.g. as a collider.
- **B. `TryMonotonePartition` with holes can return pieces that are neither simple nor monotone**
  (about 7% of random polygons with holes; `TryTriangulateMONO` then returns `false`).
  Workaround: prefer `TryTriangulateEC` for polygons with holes, or validate the MONO pieces.
- **C. MONO accepts a counter-clockwise hole and fills it**: `TryTriangulateMONO` and
  `TryMonotonePartition` triangulate the hole as solid (EC and HM return `false`).
  Workaround: call `SetOrientation(PolygonOrientation.Clockwise)` on every hole before MONO.

Repro inputs and numbers: [dogfooding round 1](https://github.com/HanJaeJoon/PolyPartitionSharp/blob/main/docs/dogfooding/round1.md#upstream-defects-reproduced-by-the-port).

## License

MIT. The original polypartition is Copyright (c) 2011-2021 Ivan Fratric and contributors, under the
MIT license; see [LICENSE](LICENSE).
