# Rin

A C# game engine on Vulkan, with shaders written in C# (Rin.Shade).

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/en-us/download)
- [Vulkan SDK](https://www.lunarg.com/vulkan-sdk/)
- [uv](https://docs.astral.sh/uv/), which installs Python and [silo](https://github.com/TareHimself/silo) for the native modules
- A C++ toolchain, to build the native modules

## First-time setup

The native libraries come from a local NuGet feed (`.feed/`). Build them once:

```
uv run silo profile detect
uv run python scripts/pack_native.py
```

The first run takes a while. After changing a module's C++, rebuild just that one: `uv run python scripts/pack_native.py Rin.Native`.

## Build, run, test

```
dotnet build rin.sln
dotnet run --project Examples/Examples/Examples.csproj -- scene-test
dotnet test Engine/Rin.Core.Tests/Rin.Core.Tests.csproj -p:RinShadeSkipCompile=true
```

Run the examples project without a name for a launcher. `-p:RinShadeSkipCompile=true` skips shader compilation for tests that don't load shaders. See [native/ReadMe.md](native/ReadMe.md) for real, stub and fake native libraries.

## Layout

| Folder | What's in it |
| --- | --- |
| `Engine/` | Rin.Core, Rin.World, graphics and audio backends, glTF, source generators |
| [`Shade/`](Shade/ReadMe.md) | The C# to Slang shader transpiler |
| `Slang/` | Managed Slang compiler wrapper and CLI |
| [`native/`](native/ReadMe.md) | C++ modules, their packaging, and test fakes |
| `Examples/`, `Experiments/` | Demo apps and prototypes |
| `msbuild/`, `scripts/` | Shared shader build targets and CI helpers |

[ARCHITECTURE.md](ARCHITECTURE.md) shows how the pieces fit. [AGENTS.md](AGENTS.md) has contributor conventions.

## All READMEs

**Engine**

- [Rin.Audio.Miniaudio.Tests](Engine/Rin.Audio.Miniaudio.Tests/ReadMe.md): NUnit tests for the miniaudio backend against a recording engine, no native library needed.
- [Rin.Audio.Miniaudio](Engine/Rin.Audio.Miniaudio/ReadMe.md): Audio backend built on miniaudio, implementing `IAudioModule` from Rin.Core.
- [Rin.Audio.Null](Engine/Rin.Audio.Null/ReadMe.md): An audio backend that does nothing, for headless runs and tests.
- [Rin.Core.Tests](Engine/Rin.Core.Tests/ReadMe.md): NUnit tests for Rin.Core.
- [Rin.Core](Engine/Rin.Core/ReadMe.md): The base library: views, the graphics graph, audio interfaces and shared types.
- [Rin.GLTF.Tests](Engine/Rin.GLTF.Tests/ReadMe.md): NUnit tests for Rin.GLTF.
- [Rin.GLTF](Engine/Rin.GLTF/ReadMe.md): Imports glTF/glb files into Rin meshes and animations, using `SharpGLTF.Core`.
- [Rin.Graphics.Null](Engine/Rin.Graphics.Null/ReadMe.md): A graphics backend that does nothing, for headless runs and tests.
- [Rin.Graphics.Vulkan.Tests](Engine/Rin.Graphics.Vulkan.Tests/ReadMe.md): NUnit tests for the Vulkan backend's native boundary, against a fake of `Rin.Graphics.Vulkan.Native`.
- [Rin.Graphics.Vulkan](Engine/Rin.Graphics.Vulkan/ReadMe.md): Vulkan backend for the `IGraphicsModule` interface from Rin.Core.
- [Rin.SourceGenerators.Tests](Engine/Rin.SourceGenerators.Tests/ReadMe.md): xUnit tests for the generators in Rin.SourceGenerators.
- [Rin.SourceGenerators](Engine/Rin.SourceGenerators/ReadMe.md): Roslyn incremental source generators, referenced as an analyzer by Rin.Core, Rin.World and the projects that declare ECS components.
- [Rin.World.AotSmoke](Engine/Rin.World.AotSmoke/ReadMe.md): A console app published with NativeAOT that checks the ECS runs without reflection.
- [Rin.World.Tests](Engine/Rin.World.Tests/ReadMe.md): NUnit tests for Rin.World.
- [Rin.World](Engine/Rin.World/ReadMe.md): The scene layer: an ECS world with render, physics and script features, and the default render pipeline.

**Shade** ([overview](Shade/ReadMe.md))

- [Rin.Shade.Cli](Shade/Rin.Shade.Cli/ReadMe.md): Command line wrapper over the transpiler.
- [Rin.Shade.CpuTests](Shade/Rin.Shade.CpuTests/ReadMe.md): Transpiles a shader, runs it on the CPU through Slang and compares the result with System.Numerics.
- [Rin.Shade.MSBuild](Shade/Rin.Shade.MSBuild/ReadMe.md): The MSBuild task `Rin.Shade.MSBuild.CompileRinShadeShaders`.
- [Rin.Shade.SourceGenerator](Shade/Rin.Shade.SourceGenerator/ReadMe.md): Roslyn incremental source generators for Rin.Shade.
- [Rin.Shade.Tests](Shade/Rin.Shade.Tests/ReadMe.md): NUnit tests for the transpiler and the source generators.
- [Rin.Shade.Transpiler](Shade/Rin.Shade.Transpiler/ReadMe.md): Roslyn-based transpiler from C# shader classes to Slang source text.
- [Rin.Shade](Shade/Rin.Shade/ReadMe.md): The C# authoring API for shaders.
- [Samples](Shade/Samples/ReadMe.md): Three tiny assemblies that exist only to test cross-assembly shader transpilation.

**Slang**

- [Rin.Slang.Cli](Slang/Rin.Slang.Cli/ReadMe.md): Command line tool, built as the executable `rin-slang`.
- [Rin.Slang.Compiler.Tests](Slang/Rin.Slang.Compiler.Tests/ReadMe.md): NUnit tests for `ShaderCompiler`.
- [Rin.Slang.Compiler](Slang/Rin.Slang.Compiler/ReadMe.md): Compiles `.slang` source using the native Slang wrapper.
- [Rin.Slang.Discovery](Slang/Rin.Slang.Discovery/ReadMe.md): Finds which shaders C# code references, without compiling the C# code.
- [Rin.Slang.Tests](Slang/Rin.Slang.Tests/ReadMe.md): NUnit tests for `Rin.Slang` (the package format).
- [Rin.Slang](Slang/Rin.Slang/ReadMe.md): The compiled shader package format, with no native dependency.

**native** ([overview](native/ReadMe.md))

- [Fakes](native/Fakes/ReadMe.md): C# projects that are published with NativeAOT into real native libraries, standing in for the C++ modules in tests.
- [Rin.Audio.Miniaudio.Native](native/Rin.Audio.Miniaudio.Native/ReadMe.md): Native wrapper over miniaudio.
- [Rin.Graphics.Vulkan.Native](native/Rin.Graphics.Vulkan.Native/ReadMe.md): Native layer for the Vulkan backend.
- [Rin.Native](native/Rin.Native/ReadMe.md): Core native helpers for the engine.
- [Rin.Slang.Native](native/Rin.Slang.Native/ReadMe.md): Small C++ wrapper that exposes the Slang compiler to C#.

**Examples**

- [Common](Examples/Common/ReadMe.md): Shared library for the example and experiment apps, not runnable on its own.
- [Examples](Examples/Examples/ReadMe.md): Every example app in one executable, picked by name or from a launcher window.
- [Examples.Tests](Examples/Examples.Tests/ReadMe.md): NUnit tests for the audio player library and the render graph overlay's layout.

**Experiments**

- [experiment.FontIcon](Experiments/experiment.FontIcon/ReadMe.md): Smoke test for FontIconView using the Font Awesome 6 Free solid icon font.
- [experiment.Slug](Experiments/experiment.Slug/ReadMe.md): Demonstrates SLUG GPU vector rendering through a `CanvasView`.
- [experiment.StencilAndCover](Experiments/experiment.StencilAndCover/ReadMe.md): GPU stencil-and-cover path filling demo through a CanvasView.
- [experiments.Docking](Experiments/experiments.Docking/ReadMe.md): Docking system demo: one window with an initial docked layout.
