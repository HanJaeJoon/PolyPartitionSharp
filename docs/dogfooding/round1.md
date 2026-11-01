# PolyPartitionSharp round 1 dogfooding findings

Target: `PolyPartitionSharp 0.1.0` from this branch (`src/PolyPartitionSharp`), consumed through a
`ProjectReference` by a net8.0 console app in `%TEMP%\dogfood-PolyPartitionSharp` (`dotnet run -c Release -- 1..7`).
Library source was not opened while writing the scenarios - only `README.md` and the generated XML docs.
Source was opened afterwards, only to attribute the failures found.

Focus: polygons with holes (triangulation and convex partitioning), CW/CCW handling, collinear and
duplicate points, and using the output as a game mesh index buffer.

## Scenarios

- [x] 1. Game level floor: L-shaped room with two pillar holes -> vertex buffer + index buffer
- [x] 2. Winding: wrong outer/hole orientation, `GetOrientation`/`SetOrientation` fix-up
- [x] 3. Collinear and duplicate points (edge midpoints, combs, GeoJSON-style closing vertex, spikes, holes touching the outer)
- [x] 4. Degenerate and extreme input (0-3 points, collinear, bowtie, NaN/Infinity, 1e-20..1e15 scale, null, aliasing)
- [x] 5. Performance and allocations (n = 100..4000, OPT up to n = 200, up to 400 holes)
- [x] 6. Public surface audit (reflection over exported types)
- [x] 7. Randomized cross-check: 3000 star polygons (2/3 float, 1/3 integer-snapped), 0-2 holes each, every algorithm
- [x] 8. (added) Differential test against upstream C++ on the scenario 1-4 and scenario 7 inputs

Validation used throughout: for triangulations, every directed edge must pair with its reverse or be an
input boundary edge (each boundary edge exactly once), every triangle CCW with non-zero area, and the
area sum equal to outer minus holes. Convex partitions: area sum, convexity and CCW of each part.

---

# Verdict: 0.1.0 can ship. No port bugs were found; three upstream defects are reproduced faithfully and should be documented first (P0).

Every difference against upstream was zero once upstream was built the way the port computes
(see "Upstream differential"), including in the cases where both produce wrong output. So nothing in
this round is a porting bug, and no library code was changed. What a consumer will trip over is
upstream behavior that the README does not warn about: two algorithms can return `true` with invalid
output, and one returns `true` and fills a hole when the hole has the wrong winding.

## Upstream differential (scenario 8)

Upstream `polypartition` master `b000a4a` built with `zig c++ -O2`, fed the same inputs, outputs compared
coordinate for coordinate:

| algorithm | cases | return differs | output differs |
|---|---|---|---|
| EC | 3030 | 0 | 0 |
| MONO | 3030 | 0 | 1 (both `false`, different partial output) |
| OPT | 1018 | 0 | 0 |
| HM | 3030 | 0 | 0 |
| ConvexPartition OPT | 1018 | 0 | 0 |
| MonotonePartition | 3030 | 0 | 1 (exact duplicate vertex) |

The two remaining differences come from an input with an exact duplicate vertex: the vertex sort sees a
tie, and `std::sort` and `Array.Sort` order ties differently. Upstream leaves that order unspecified too.

Two things had to be fixed in the C++ build to get there, and anyone repeating this needs them:

- **FMA contraction.** With the default `-ffp-contract`, clang fuses multiply-adds, and EC/HM picked
  different ears in 350 and 286 of 3000 cases (same return, different triangle order). With
  `-ffp-contract=off -mcpu=baseline` the difference is 0. The port matches strict IEEE upstream.
- **`long` width.** `ConvexPartition_OPT` stores weights in `long` starting at `2147483647`. With a 32-bit
  `long` (Windows/MSVC ABI) `w + 1` overflows and upstream returned `false` in 153/1000 cases where the
  port returns `true`. The port uses a 64-bit `long` and matches upstream on LP64 (Linux/macOS), which
  is where the bundled fixtures match too.

## Upstream defects reproduced by the port

### A. [silent wrong output] `TryConvexPartitionOPT` returns non-convex parts

In 56 of 1000 random simple polygons (23/650 float, 33/350 integer) it returns `true` with at least one
non-convex part. Areas always add up, so a caller only notices by checking convexity. Upstream built
for LP64 produces the identical parts. Example: the 21-vertex polygon
`(8,1) (6,3) (6,5) (5,8) (2,8) (0,5) (-2,6) (-5,8) (-6,5) (-8,3) (-9,1) (-8,-2) (-7,-4) (-6,-6) (-4,-9) (-2,-10) (2,-9) (3,-9) (6,-6) (7,-4) (6,-1)`
yields 5 parts, one of which (`(2,8) (0,5) (-2,6) (6,-1)`) is reflex at `(0,5)`. For physics colliders this is
the output that breaks things.

### B. [silent wrong output] `TryMonotonePartition` with holes returns invalid pieces

92 of 1350 float polygons with holes (6.8%) return `true` with a piece that visits the same vertex
up to 3 times and has 2 local y-maxima, so it is neither simple nor monotone. Area sums still match.
`TryTriangulateMONO` builds on this and then returns `false` for the same 92 inputs, while EC triangulates
all 1350. Rotating the holes 0.1 rad so they have no horizontal edges does not help (96 failures), so
it is not a horizontal-edge tie. Without holes: 0 failures.

### C. [silent wrong output] MONO accepts a CCW hole and fills it

Outer CCW 4x4 square, hole given CCW instead of CW: `TryTriangulateMONO` and `TryMonotonePartition` return
`true` with area 20 (16 + 4: the hole is triangulated as a second solid polygon on top of the outer).
EC and HM return `false` for the same input. Wrong outer winding fails cleanly in every algorithm.

## What was verified as working

**Game mesh (scenario 1).** L-shaped room (6 vertices) + 2 pillars (4 each), ids 0..13 as vertex buffer
indices. EC and MONO: 16 triangles = n + 2h - 2 exactly, 48 indices, 0 out of range, all CCW, area 81
(analytic), every interior edge shared by exactly two triangles. HM: 7 convex parts. `TryRemoveHoles`
returns one CCW polygon of 18 points (2 bridge vertices duplicated per hole, ids preserved), so the
index buffer flow survives hole removal. The `PolygonPoint.Id` mapping is what makes this work: the
`Vector2` constructor gives every vertex id 0, so its output cannot be turned into an index buffer.

**Winding (scenario 2).** `GetOrientation` reports CCW/CW/None correctly (None for collinear).
`SetOrientation(CounterClockwise)` on the outer + `SetOrientation(Clockwise)` on the hole fixes every
wrong-winding combination. Wrong outer winding returns `false` from all six algorithms. Exception: C above.

**Collinear points (scenario 3).** Edge midpoints (4 collinear triples) and an 11-vertex collinear comb:
EC, MONO, OPT and HM all valid with 0 degenerate triangles; HM merges back to 1 part. Two holes whose
rightmost vertices share an x: all valid.

**Extreme input (scenario 4).** 0 and 2 points return `false` everywhere. Unit square at 1e6 offset,
L shape at 1e-6 and 1e15 scale: all valid. Hole outside the outer and a lone hole: `false` everywhere.
Null polygon / null list / null element throw `ArgumentNullException` / `ArgumentException` naming the
parameter. Inputs are never mutated, outputs never alias inputs, lazy `IEnumerable` input works.

**Random input (scenario 7).** EC and HM: 0 invalid results in 3000 inputs, with or without holes;
`false` only on integer inputs (EC 6/1000, HM 6/1000; the ones inspected have exact duplicate vertices). OPT
triangulation: 0 invalid. HM never produced fewer parts than ConvexPartition OPT (0/998).

## Degenerate input that is out of contract but common

All identical to upstream. These are not defects (input is not a simple polygon), but the README does
not say what "valid input" means, and some of these inputs are what file formats produce:

| input | EC | MONO | OPT | HM | ConvexPartition OPT | MonotonePartition |
|---|---|---|---|---|---|---|
| closing duplicate (last == first, GeoJSON/SVG) | `true`, 1 zero-area tri | `false` | `true`, 1 zero-area tri | ok (square), zero-area part (L shape) | `false` (square), ok (L shape) | `false` |
| consecutive duplicate vertex | `true`, 1 zero-area tri | `false` | `false` | ok | `false` | ok |
| zero-width spike | `true`, 1 CW tri | `false` | `false` | `true`, non-convex part | `false` | `false` |
| hole vertex on outer edge | `true`, 1 zero-area tri | `true`, 1 zero-area tri | n/a | `true`, degenerate part | n/a | ok |
| hole shares an outer vertex | `true`, CW tri + broken edge | `false` | n/a | `true`, non-convex part | n/a | `false` |
| bowtie | `true`, CW tri | `false` | `false` | `true`, non-convex part | `false` | `false` |
| NaN vertex | `false` | `false` | `true`, NaN area | `true`, NaN area | `false` | `false` |
| Infinity vertex | `true`, NaN area | `true`, NaN area | `true`, NaN area | `true`, NaN area | `false` | `true`, NaN area |
| all 5 points collinear | `false` | `false` | `false` | `true`, 1 zero-area part | `false` | `false` |

Integer-snapped random polygons (collinear and duplicate points are common): OPT triangulation returns
`false` for 116/350 (33%), MONO 15/350, EC 1/350.

## Performance (scenario 5)

Release, net8.0, star polygons (`Stopwatch`, `GC.GetAllocatedBytesForCurrentThread`; single-shot for the
largest sizes, so treat timings as +-30%):

| algorithm | n | ms | KB/call | B/vertex |
|---|---|---|---|---|
| EC | 100 / 1000 / 4000 | 0.24 / 11.9 / 72 | 18 / 172 / 689 | 176-180 |
| MONO | 100 / 1000 / 4000 | 0.03 / 1.3 / 1.1 | 33 / 332 / 1326 | 336-339 |
| HM | 100 / 1000 / 4000 | 0.29 / 8.0 / 116 | 23 / 219 / 882 | 224-235 |
| OPT | 50 / 100 / 200 | 0.40 / 1.8 / 12.5 | 38 / 135 / 505 | 782-2584 |
| ConvexPartition OPT | 50 / 100 / 200 | 1.4 / 2.7 / 14.9 | 217 / 757 / 2879 | 4434-14742 |

Holes (4-vertex outer + k square holes, k = 10 / 100 / 400, n = 44 / 404 / 1604):

| algorithm | ms | KB/call |
|---|---|---|
| EC | 0.09 / 19.5 / 627 | 17 / 486 / 6159 |
| MONO | 0.007 / 0.07 / 3.9 | 18 / 170 / 678 |
| HM | 0.08 / 22.8 / 606 | 20 / 520 / 6304 |

Complexities match the README. EC and HM grow as O(h n^2) through `TryRemoveHoles`: at 400 holes EC
is 160x slower than MONO. Allocation is linear in n for EC/MONO/HM; every call allocates its result
lists, since there is no overload that fills a caller-owned list.

## Public surface (scenario 6)

4 exported types: `Polygon` (sealed, `IReadOnlyList<PolygonPoint>`), `PolygonPoint` (readonly struct),
`PolygonOrientation` (enum, `Clockwise = -1, None = 0, CounterClockwise = 1`), `PolyPartition` (static,
10 `Try*` methods). Nothing internal leaks (`DPState2`, `PartitionVertex`, `Diagonal`, `MonotoneVertex`,
`ScanLineEdge`, `EdgeTree`, `VertexType` are all internal). Friction:

- `TryTriangulateEC/OPT/MONO`, `TryConvexPartitionHM/OPT`: all-caps suffixes break the .NET naming
  guideline (acronyms of 3+ letters are Pascal-cased: `Mono`, `Opt`). They come from upstream names.
  Renaming is cheapest before 0.1.0 and needs a decision, so nothing was changed.
- `new Polygon([new(0, 0), ...])` does not compile (CS0121): target-typed `new` is ambiguous between the
  `IEnumerable<Vector2>` and `IEnumerable<PolygonPoint>` constructors. The README uses `new Vector2(...)`, so it is fine as documented.
- `Polygon.IsHole` and the indexer are settable, but the XML docs say "Whether the polygon is a hole"
  and do not mention the setter.
- `Polygon.IsValid` only means "at least 3 points"; the name suggests a simple-polygon check.
- `PolygonPoint` does not implement `IEquatable<PolygonPoint>`, so `Equals` and `HashSet` go through `ValueType.Equals`.
- The class remarks say partial output on failure is "as upstream leaves it". True structurally, but
  with duplicate vertices the partial output depends on sort tie order (see differential).

## Recommended changes

### P0 (before 0.1.0, docs only)

1. README "Known limitations (inherited from upstream)": defects A, B, C above, each with the one-line
   workaround (check convexity of `TryConvexPartitionOPT` parts; prefer EC for polygons with holes or
   validate MONO pieces; call `SetOrientation` on every hole before MONO).
2. README "Input requirements": simple polygon, no repeated closing vertex, no consecutive duplicates,
   finite coordinates, holes strictly inside the outer. Point at the table above for what happens otherwise.

### P1

3. Decide whether to fix defects A and B in the port. That means deviating from upstream and from
   "ported line for line"; if yes, each needs a failing test from the inputs in this document first.
4. Decide on renaming the `EC/OPT/MONO/HM` suffixes (public API, see surface audit). Free now, a breaking change after 0.1.0.
5. README: add the index buffer recipe from scenario 1 (`PolygonPoint` ids as vertex indices,
   `SelectMany(t => t.Select(p => p.Id))`), and note that the `Vector2` constructor sets every id to 0.
6. README: a line on choosing an algorithm for many holes (MONO is 160x faster than EC at 400 holes,
   but see B).

### P2

7. XML docs: "Gets or sets" on `IsHole` and the indexer; say that `IsValid` only checks the point count;
   note that `TryConvexPartitionOPT` also returns `false` for polygons with collinear vertices.
8. `PolygonPoint : IEquatable<PolygonPoint>`.
9. (beyond the port, list only) Overloads that write into a caller-supplied `List<Polygon>` for per-frame use;
   rejecting non-finite coordinates up front.

## Resolved after round 1

Decision: keep the upstream names and behavior; defects A and B stay unfixed in code.

- Done (docs only): P0-1 and P0-2 (README "Known limitations (inherited from upstream)" and
  "Input requirements"), P1-5 (README index buffer recipe), P1-6 (README line on many holes),
  P2-7 (XML docs, plus notes on defects A/B/C in the affected methods).
- Not doing: P1-3 (fixing A/B), P1-4 (renaming the suffixes), P2-8 (`IEquatable`), P2-9 (overloads,
  finite-coordinate checks).

## Assets

`%TEMP%\dogfood-PolyPartitionSharp` holds the scenarios (`dotnet run -c Release -- 1..7`), `Compare.cs`
(differential test, `-- c <upstream.exe>` for the random corpus, `-- c2 <upstream.exe>` for scenario 1-4
inputs) and `Probe.cs` (the defect counts above). The upstream harness reads
`<algo> <npolys>` then `<hole> <n> x y ...` per polygon on stdin.
