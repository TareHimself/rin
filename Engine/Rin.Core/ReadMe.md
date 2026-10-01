# Rin.Core

The base library of the engine. It defines the application and module model, the graphics and audio interfaces that backends implement, the views (UI) system, and shared utilities. Package id `TareHimself.Rin.Core`.

## Where it fits

- References: `Rin.SourceGenerators` (as an analyzer), `Shade/Rin.Shade` and `Shade/Rin.Shade.SourceGenerator`, plus NuGet packages including NetVips, SQLite, HarfBuzzSharp and `TareHimself.Rin.Native`.
- Referenced by: Rin.World, Rin.Graphics.Vulkan, Rin.Graphics.Null, Rin.Audio.Miniaudio, Rin.Audio.Null, Rin.Core.Tests and Rin.SourceGenerators.Tests.

## Start here

- `Application.cs`, `IApplication.cs`, `IModule.cs`: application lifetime and modules.
- `Graphics/IGraphicsModule.cs`, `Graphics/IDevice.cs`, `Graphics/IRenderer.cs`: the graphics abstraction the backends implement.
- `Audio/IAudioModule.cs` and `Audio/Effects/`: the audio abstraction.
- `Views/ViewsModule.cs`, `Views/View.cs`: the UI system.
- `Shared/`: math, curves, pooling, logging, providers, dispatcher and other utilities.
- `Animation/`, `Archives/`, `Sources/`: animations, SQLite-backed archives, and content sources.

## Build and test

```
dotnet build Engine/Rin.Core/Rin.Core.csproj
dotnet test Engine/Rin.Core.Tests/Rin.Core.Tests.csproj -p:RinShadeSkipCompile=true
```

## Notes

- `Content/**` is embedded as resources. Shaders under `Shaders/Rin/Core/` are compiled by the Rin.Shade MSBuild targets into `Content\Shaders\Rin\Core\`. CI skips that compilation for tests with `-p:RinShadeSkipCompile=true`.
- Unsafe code is enabled and the project is marked AOT compatible.
