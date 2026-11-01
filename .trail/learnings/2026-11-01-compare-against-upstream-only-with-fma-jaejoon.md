---
title: "Compare against upstream only with FMA off and a 64-bit long; wrong OPT/MONO output is upstream's"
author: Jaejoon
date: 2026-11-01
branch: HanJaeJoon/dogfood-r1
cost: { minutes: 14, tokens: 211444 }
id: l-094f2d4b
scope: repo
status: active
src: agent
touches:
  - docs/dogfooding/round1.md
  - src/PolyPartitionSharp/PolyPartition.ConvexPartitionOPT.cs
  - src/PolyPartitionSharp/PolyPartition.Monotone.cs
---

## Decided
The port is checked against upstream polypartition built as strict IEEE on an LP64 ABI. Under that build every output matched on 3000 random inputs plus the dogfood scenario inputs, so differences seen under any other build are build artifacts, not port bugs.

## Watch out
- clang/zig fuses multiply-adds by default; EC and HM then pick different ears in ~10% of cases (same return value, different triangle order). Build upstream with -ffp-contract=off -mcpu=baseline.
- ConvexPartition_OPT keeps weights in `long` starting at 2147483647. With a 32-bit long (Windows/MSVC) w + 1 overflows and upstream returns 0 in ~15% of cases. The port uses 64-bit long, matching Linux/macOS upstream and the bundled fixtures.
- These are upstream defects the port reproduces on purpose, not port bugs: ConvexPartition_OPT can return true with a non-convex part (~5.6% of random polygons); MonotonePartition with holes can return true with a non-simple piece (~6.8%), which makes Triangulate_MONO return false; MONO accepts a CCW hole and fills it. Fixing any of them means deviating from upstream, which needs the maintainer's decision.

In the code: src/PolyPartitionSharp/PolyPartition.ConvexPartitionOPT.cs (DPState2.weight is long), src/PolyPartitionSharp/PolyPartition.Monotone.cs. Numbers and repro inputs: docs/dogfooding/round1.md.
