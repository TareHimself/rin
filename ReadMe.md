# Rin

## Prerequisites
- [.NET SDK 10](https://dotnet.microsoft.com/en-us/download) - pinned in `global.json` (`rollForward: latestMajor`); every project targets `net10.0` (set in `Directory.Build.props`)
- [Vulkan SDK](https://www.lunarg.com/vulkan-sdk/)
- [uv](https://docs.astral.sh/uv/) - installs Python and the native build tools (Conan, CMake and Task) from `pyproject.toml`, so you do not install those yourself
- A C++ toolchain, only if you build the native modules: Visual Studio Build Tools with "Desktop development with C++" on Windows, or gcc or clang on Linux and macOS

## First-time setup
The managed projects pull the native libraries (Vulkan graphics backend, Miniaudio, the Slang shader compiler, etc.) as NuGet packages from a local feed (`.feed/`, wired up in `NuGet.Config`). Build and pack them once before opening the solution:

```
uv run task pack-all
```

This builds each native module (via Conan + CMake) and `dotnet pack`s it into `.feed/`. Re-run the relevant task (`pack-native`, `pack-audio-miniaudio-native`, `pack-graphics-vulkan-native`, `pack-slang-native`) whenever you change that module's C++ source.

## Building
```
dotnet build rin.sln
```

## Running an example
Pick any project under `Examples/` (`SceneTest`, `Sponza`, `AudioPlayer`, `ViewsTest`, `NodeGraphTest`, `RLTest`, `ChatApp`):

```
dotnet run --project Examples/SceneTest/SceneTest.csproj
```

Or open `rin.sln` in Visual Studio/Rider and run an Examples project directly.

## Testing
Each test project runs on its own, for example:

```
dotnet test Engine/Rin.Core.Tests/Rin.Core.Tests.csproj -p:RinShadeSkipCompile=true
dotnet test Shade/Rin.Shade.Tests/Rin.Shade.Tests.csproj
```

`-p:RinShadeSkipCompile=true` skips compiling the shaders through the native Slang compiler, which tests that never load shader content do not need. `Shade/Rin.Shade.CpuTests` runs transpiled shaders on the CPU and needs the real native Slang package. CI (`.github/workflows/ci.yml`) runs one job per test project, plus a job that builds the real native Slang and runs the tests and the full shader compile that need it. See `native/ReadMe.md` for how real, stub and fake native libraries differ.

## Shaders
Shaders are written in C# with Rin.Shade, which transpiles them to Slang at build time (`Shade/ReadMe.md`). There are no hand-written `.slang` sources in the repo.

## Repository layout
Each project has its own `ReadMe.md`, and `Shade/` and `native/` have one for the subsystem as a whole.

- `Engine/`: core engine (`Rin.Core`, `Rin.World`, graphics and audio backends, glTF loading, source generators) and its tests
- [`Shade/`](Shade/ReadMe.md): Rin.Shade, the C# to Slang shader transpiler, with its source generator, MSBuild task and tests
- `Slang/`: managed Slang compiler wrapper and command line tool
- [`native/`](native/ReadMe.md): native C++ modules, their packaging, and the fakes used in CI
- `Examples/`: sample and demo apps
- `Experiments/`: prototype projects
- `msbuild/`: MSBuild targets shared by the shader projects (`RinShade.targets`)
- `scripts/`: CI helpers (native stub packages, native fakes)

How the pieces fit together is in [`ARCHITECTURE.md`](ARCHITECTURE.md). Conventions for contributors and coding agents are in [`AGENTS.md`](AGENTS.md), with extra rules for [`Shade/`](Shade/AGENTS.md) and [`native/`](native/AGENTS.md).

## All READMEs

**Engine**

- [Rin.Audio.Miniaudio](Engine/Rin.Audio.Miniaudio/ReadMe.md): Audio backend built on miniaudio, implementing `IAudioModule` from Rin.Core.
- [Rin.Audio.Null](Engine/Rin.Audio.Null/ReadMe.md): An audio backend that does nothing, for headless runs and tests.
- [Rin.Core.Tests](Engine/Rin.Core.Tests/ReadMe.md): NUnit tests for Rin.Core.
- [Rin.Core](Engine/Rin.Core/ReadMe.md): The base library: views, the graphics graph, audio interfaces and shared types.
- [Rin.GLTF.Tests](Engine/Rin.GLTF.Tests/ReadMe.md): NUnit tests for Rin.GLTF.
- [Rin.GLTF](Engine/Rin.GLTF/ReadMe.md): Imports glTF/glb files into Rin meshes and animations, using `SharpGLTF.Core`.
- [Rin.Graphics.Null](Engine/Rin.Graphics.Null/ReadMe.md): A graphics backend that does nothing, for headless runs and tests.
- [Rin.Graphics.Vulkan](Engine/Rin.Graphics.Vulkan/ReadMe.md): Vulkan backend for the `IGraphicsModule` interface from Rin.Core.
- [Rin.SourceGenerators.Tests](Engine/Rin.SourceGenerators.Tests/ReadMe.md): xUnit tests for the generators in Rin.SourceGenerators.
- [Rin.SourceGenerators](Engine/Rin.SourceGenerators/ReadMe.md): Roslyn incremental source generators, referenced as an analyzer by Rin.Core and Rin.World.
- [Rin.World.Tests](Engine/Rin.World.Tests/ReadMe.md): NUnit tests for Rin.World.
- [Rin.World](Engine/Rin.World/ReadMe.md): The scene layer: actors, components, physics and the default render pipeline.

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

- [AssetViewer](Examples/AssetViewer/ReadMe.md): Loads the fox model and loops its "Run" animation clip.
- [AudioPlayer](Examples/AudioPlayer/ReadMe.md): Audio player UI (track player, visualizer, file picker) built on the views and Miniaudio modules.
- [ChatApp](Examples/ChatApp/ReadMe.md): Chat UI views. Scratch code: the module that hosts them is commented out.
- [Common](Examples/Common/ReadMe.md): Shared library for the example and experiment apps, not runnable on its own.
- [HeadlessTest](Examples/HeadlessTest/ReadMe.md): Runs the engine with `NullGraphicsModule` and `NullAudioModule` (no window, no GPU).
- [NodeGraphTest](Examples/NodeGraphTest/ReadMe.md): Node graph views with typed pins and connections.
- [P2PChat](Examples/P2PChat/ReadMe.md): Peer to peer text chat over TCP, one instance hosts and others connect.
- [P2PChat.Tests](Examples/P2PChat.Tests/ReadMe.md): NUnit tests for P2PChat's framing, address parsing and loopback host and client sessions.
- [RLTest](Examples/RLTest/ReadMe.md): Scratch console program for HostImage that loads a JPEG and saves a PNG.
- [SceneTest](Examples/SceneTest/ReadMe.md): 3D world with physics in a dockable layout with perspective and top cameras.
- [Sponza](Examples/Sponza/ReadMe.md): Loads sponza.glb and shows it through a viewport with a custom Shade mesh material.
- [UiGallery](Examples/UiGallery/ReadMe.md): Gallery of UI rendering features: every quad mode, blur and clipping, laid out in sections that wrap to the window width.
- [RenderGraphOverlay](Examples/RenderGraphOverlay/ReadMe.md): Library with a debug overlay that opens from a corner button and draws the render graph of a captured frame, using reflection and a probe pass, with no engine changes.
- [RenderGraphOverlay.Tests](Examples/RenderGraphOverlay.Tests/ReadMe.md): NUnit tests for the overlay's graph layout.
- [RenderGraphViewer](Examples/RenderGraphViewer/ReadMe.md): Sample app that opens the render graph overlay over a small scene.
- [ViewsTest](Examples/ViewsTest/ReadMe.md): Test bench for views, animation, images and audio effects (parametric EQ, stress-test delay, bloom).

**Experiments**

- [experiment.FontIcon](Experiments/experiment.FontIcon/ReadMe.md): Smoke test for FontIconView using the Font Awesome 6 Free solid icon font.
- [experiment.Slug](Experiments/experiment.Slug/ReadMe.md): Demonstrates SLUG GPU vector rendering through a `CanvasView`.
- [experiment.StencilAndCover](Experiments/experiment.StencilAndCover/ReadMe.md): GPU stencil-and-cover path filling demo through a CanvasView.
- [experiments.Docking](Experiments/experiments.Docking/ReadMe.md): Docking system demo: one window with an initial docked layout.
