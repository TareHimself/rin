# Rin.World.Tests

NUnit tests for [Rin.World](../Rin.World/ReadMe.md). Folders mirror the subsystem folders of Rin.World, shared fakes stay in `TestDoubles.cs` at the root, and `World` itself is tested at the root (the rules are in `AGENTS.md`).

```
dotnet test Engine/Rin.World.Tests/Rin.World.Tests.csproj -p:RinShadeSkipCompile=true
```

CI passes `-p:RinShadeSkipCompile=true` for this project.

| Folder | Covers |
| --- | --- |
| `Components/` | Light, skinned mesh and transform components, and contact event routing from the fake physics system to components |
| `Graphics/` | The default render system, its view data, skinned proxy poses and render proxy handles |
| `Physics/` | Queries, contact and trigger events, mesh and hull bodies and time dilation against the real Bepu system, plus body handles and the world facade against the fakes |
| `Skinning/` | Skeleton poses, animation clips and the animation graph nodes (mirrors `Mesh/Skinning/`) |

References: Rin.World (Rin.Core comes through it).
