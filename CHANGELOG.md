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

[Unreleased]: https://github.com/HanJaeJoon/PolyPartitionSharp/commits/main
