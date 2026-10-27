# Rin.Core.Tests

NUnit tests for [Rin.Core](../Rin.Core/ReadMe.md). Folders mirror the subsystem folders of Rin.Core (the rule is in `AGENTS.md`).

```
dotnet test Engine/Rin.Core.Tests/Rin.Core.Tests.csproj -p:RinShadeSkipCompile=true
```

CI passes `-p:RinShadeSkipCompile=true` for this project.

| Folder | Covers |
| --- | --- |
| `Extensions/` | Stream read and write helpers |
| `Audio/Effects/` | The effect parameter triple buffer, including a concurrent writer and reader checking for torn reads |
| `Graphics/` | Resource handles and the frame limiter |
| `Shared/` | Bounds, buffers, math, curves, dispatcher, id factory, providers, time and video |
| `Views/` | Views, layouts, colour and border radius value types, blur and clip commands and SDF glyph outlines, with shared fakes in `Views/TestDoubles.cs` |

References: Rin.Core.
