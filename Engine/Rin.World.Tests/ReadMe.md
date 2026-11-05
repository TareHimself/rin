# Rin.World.Tests

NUnit tests for [Rin.World](../Rin.World/ReadMe.md). Folders mirror the subsystem folders of Rin.World, shared fakes stay in `TestDoubles.cs` at the root, and `World` itself is tested at the root (the rules are in `AGENTS.md`).

```
dotnet test Engine/Rin.World.Tests/Rin.World.Tests.csproj -p:RinShadeSkipCompile=true
```

CI passes `-p:RinShadeSkipCompile=true` for this project.

| Folder | Covers |
| --- | --- |
| root | `World`: entity handles, the hierarchy, hooks and the command buffer |
| `Ecs/` | Queries over the Friflo backend and their zero-allocation check |
| `Scheduling/` | The tick and frame scheduler, time scale and interpolation alpha |
| `Transforms/` | Transform setters and propagation |
| `Graphics/` | Render proxies for entities (static, skinned, lights), extract and camera views, plus the default render system, its view data, skinned proxy poses and render proxy handles |
| `Animation/` | Pose source animation, notifies and bone sockets |
| `Physics/` | Entity bodies, push and pull, contacts, entity queries, time scale and a determinism check against the fakes, plus queries, contact and trigger events, mesh and hull bodies and time dilation against the real Bepu system, and body handles |
| `Scripting/` | The script lifecycle, tick order and restore |
| `Tasks/` | `GameTask` waits, combinators, resume order, cancellation, faults and the zero-allocation check |
| `Skinning/` | Skeleton poses, animation clips and the animation graph nodes (mirrors `Mesh/Skinning/`) |

References: Rin.World (Rin.Core comes through it).
