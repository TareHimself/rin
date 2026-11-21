# Rin.World.Tests

NUnit tests for [Rin.World](../Rin.World/ReadMe.md). Folders mirror the subsystem folders of Rin.World, shared fakes stay in `TestDoubles.cs` at the root, and `World` itself is tested at the root (the rules are in `AGENTS.md`).

```
dotnet test Engine/Rin.World.Tests/Rin.World.Tests.csproj -p:RinShadeSkipCompile=true
```

CI passes `-p:RinShadeSkipCompile=true` for this project.

| Folder | Covers |
| --- | --- |
| root | `World`: entity handles, archetype moves and row order, the hierarchy, hooks and the command buffer, snapshot save and restore (round trip, saved query order, zero allocation, the saved `Random`), and `DeterminismTests`: a scripted input log run twice and rolled back, hashed every tick |
| `Ecs/` | Queries and their `With` and `Without` filters, the structural change guard and the zero-allocation checks, the entity table, and `ComponentSchema`: stable ids across clones, freezing, two worlds on one schema, and that `ComponentSchema.Engine` lists every Rin.World component |
| `Scheduling/` | The tick and frame scheduler, time scale and interpolation alpha |
| `Transforms/` | Transform setters and propagation |
| `Graphics/` | Render proxies for entities (static, skinned, lights) and their rebuild after a restore, extract and camera views, plus the default render system, its view data, the `ClusterBinner` sort and z-bins per kind, `LightBinner`'s split of lights and volumes, the light grid's tile math, skinned proxy poses and render proxy handles, and environment lighting: panorama shrinking and its spherical harmonics, the reflectivity lookup table, the map store, `EnvironmentLight` proxies, the volume data shaders read and the per-view backdrop choice. Shader functions are called directly as C#: box weight and box projection, irradiance, the tone curves, cubemap face and panorama directions, GGX sampling and base reflectivity |
| `Animation/` | Pose source animation, notifies and bone sockets |
| `Physics/` | Entity bodies and their rebuild after a restore, push and pull, contacts and their order, entity queries, time scale and a determinism check against the fakes, plus queries, contact and trigger events, mesh and hull bodies and time dilation against the real Bepu system, and body handles |
| `Scripting/` | The script lifecycle, tick order, restore, and which scripts survive a snapshot restore |
| `Tasks/` | `GameTask` waits, combinators, resume order, cancellation, faults, what a snapshot restore cancels, and the zero-allocation check |
| `Mesh/Skinning/` | Skeleton poses, animation clips and the animation graph nodes |

References: Rin.World (Rin.Core comes through it), Rin.Graphics.Null for the tests that create GPU resources, Rin.SourceGenerators as an analyzer.
