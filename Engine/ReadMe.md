# Engine

The core of the Rin game engine: the shared runtime, the world/scene layer, the graphics and audio backends, the glTF importer, the Roslyn source generators and the test projects for them.

## Projects

| Project | Description |
| --- | --- |
| [Rin.Core](Rin.Core/ReadMe.md) | Application and module model, graphics and audio abstractions, views (UI), shared utilities. |
| [Rin.World](Rin.World/ReadMe.md) | World, actors, components, systems, render and physics layers (uses BepuPhysics). |
| [Rin.Graphics.Vulkan](Rin.Graphics.Vulkan/ReadMe.md) | Vulkan implementation of `IGraphicsModule`. |
| [Rin.Graphics.Null](Rin.Graphics.Null/ReadMe.md) | No-op graphics backend for headless runs and tests. |
| [Rin.Audio.Miniaudio](Rin.Audio.Miniaudio/ReadMe.md) | Miniaudio implementation of `IAudioModule`. |
| [Rin.Audio.Null](Rin.Audio.Null/ReadMe.md) | No-op audio backend. |
| [Rin.GLTF](Rin.GLTF/ReadMe.md) | glTF mesh and animation importers. |
| [Rin.SourceGenerators](Rin.SourceGenerators/ReadMe.md) | Incremental source generators used by Rin.Core and Rin.World. |
| [Rin.Core.Tests](Rin.Core.Tests/ReadMe.md) | NUnit tests for Rin.Core. |
| [Rin.World.Tests](Rin.World.Tests/ReadMe.md) | NUnit tests for Rin.World. |
| [Rin.GLTF.Tests](Rin.GLTF.Tests/ReadMe.md) | NUnit tests for Rin.GLTF, run against the null graphics backend. |
| [Rin.SourceGenerators.Tests](Rin.SourceGenerators.Tests/ReadMe.md) | xUnit tests for the source generators. |

## Dependencies

```
Rin.SourceGenerators  <- Rin.Core, Rin.World (as analyzer)
Rin.Core              <- Rin.World, Rin.Graphics.Vulkan, Rin.Graphics.Null,
                         Rin.Audio.Miniaudio, Rin.Audio.Null
Rin.World             <- Rin.GLTF
```

- Rin.Core and Rin.World also reference `Shade/Rin.Shade`, and Rin.Graphics.Vulkan references `Slang/Rin.Slang` (in the default `local_file` dependency mode).
- Rin.Graphics.Vulkan and Rin.Audio.Miniaudio use native packages from the local `.feed/`. See the root `ReadMe.md` for first-time setup.
- Rin.World does not reference a graphics or audio backend. Applications under `Examples/` pick the backends.

## Tests

Each test project runs with `dotnet test Engine/<Project>/<Project>.csproj`. See `AGENTS.md` for how tests are organized.
