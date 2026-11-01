# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Initial C# port of [polypartition](https://github.com/ivanfratric/polypartition): `Polygon`,
  `PolygonPoint` (with the upstream user `Id`), `PolygonOrientation`, and `PolyPartition` with
  `TryRemoveHoles`, `TryTriangulateEC`, `TryTriangulateOPT`, `TryTriangulateMONO`,
  `TryMonotonePartition`, `TryConvexPartitionHM`, and `TryConvexPartitionOPT`.
  Outputs match the upstream test fixtures exactly.
- README sections on input requirements, known limitations inherited from upstream (non-convex
  `TryConvexPartitionOPT` parts, invalid MONO pieces with holes, MONO filling a counter-clockwise
  hole) with workarounds, building an index buffer from point ids, and choosing an algorithm for
  many holes. The XML docs of the affected methods note the same limitations.

[Unreleased]: https://github.com/HanJaeJoon/PolyPartitionSharp/commits/main
