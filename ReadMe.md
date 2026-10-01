# Rin

## Prerequisites
- [.NET SDK 10](https://dotnet.microsoft.com/en-us/download) - pinned in `global.json` (`rollForward: latestMajor`); every project targets `net10.0` (set in `Directory.Build.props`)
- [Vulkan SDK](https://www.lunarg.com/vulkan-sdk/)
- [CMake](https://cmake.org/)
- [Python 3](https://www.python.org/)
- [Conan](https://conan.io/) (`pip install conan`) - used to fetch/build the native C++ dependencies
- [Task](https://taskfile.dev/installation/) - runs the native build/pack pipeline defined in `Taskfile.yml`

## First-time setup
The managed projects pull the native libraries (Vulkan graphics backend, Miniaudio, the Slang shader compiler, etc.) as NuGet packages from a local feed (`.feed/`, wired up in `NuGet.Config`). Build and pack them once before opening the solution:

```
task pack-all
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
Each folder and project has its own `ReadMe.md`.

- [`Engine/`](Engine/ReadMe.md): core engine (`Rin.Core`, `Rin.World`, graphics and audio backends, glTF loading, source generators) and its tests
- [`Shade/`](Shade/ReadMe.md): Rin.Shade, the C# to Slang shader transpiler, with its source generator, MSBuild task and tests
- [`Slang/`](Slang/ReadMe.md): managed Slang compiler wrapper and command line tool
- [`native/`](native/ReadMe.md): native C++ modules, their packaging, and the fakes used in CI
- [`Examples/`](Examples/ReadMe.md): sample and demo apps
- [`Experiments/`](Experiments/ReadMe.md): prototype projects
- `msbuild/`: MSBuild targets shared by the shader projects (`RinShade.targets`)
- `scripts/`: CI helpers (native stub packages, native fakes)

Conventions for contributors and coding agents are in [`AGENTS.md`](AGENTS.md).

## All READMEs

**[Engine](Engine/ReadMe.md)**
- [Rin.Audio.Miniaudio](Engine/Rin.Audio.Miniaudio/ReadMe.md)
- [Rin.Audio.Null](Engine/Rin.Audio.Null/ReadMe.md)
- [Rin.Core.Tests](Engine/Rin.Core.Tests/ReadMe.md)
- [Rin.Core](Engine/Rin.Core/ReadMe.md)
- [Rin.GLTF.Tests](Engine/Rin.GLTF.Tests/ReadMe.md)
- [Rin.GLTF](Engine/Rin.GLTF/ReadMe.md)
- [Rin.Graphics.Null](Engine/Rin.Graphics.Null/ReadMe.md)
- [Rin.Graphics.Vulkan](Engine/Rin.Graphics.Vulkan/ReadMe.md)
- [Rin.SourceGenerators.Tests](Engine/Rin.SourceGenerators.Tests/ReadMe.md)
- [Rin.SourceGenerators](Engine/Rin.SourceGenerators/ReadMe.md)
- [Rin.World.Tests](Engine/Rin.World.Tests/ReadMe.md)
- [Rin.World](Engine/Rin.World/ReadMe.md)

**[Shade](Shade/ReadMe.md)**
- [Rin.Shade.Cli](Shade/Rin.Shade.Cli/ReadMe.md)
- [Rin.Shade.CpuTests](Shade/Rin.Shade.CpuTests/ReadMe.md)
- [Rin.Shade.MSBuild](Shade/Rin.Shade.MSBuild/ReadMe.md)
- [Rin.Shade.SourceGenerator](Shade/Rin.Shade.SourceGenerator/ReadMe.md)
- [Rin.Shade.Tests](Shade/Rin.Shade.Tests/ReadMe.md)
- [Rin.Shade.Transpiler](Shade/Rin.Shade.Transpiler/ReadMe.md)
- [Rin.Shade](Shade/Rin.Shade/ReadMe.md)
- [Samples](Shade/Samples/ReadMe.md)

**[Slang](Slang/ReadMe.md)**
- [Rin.Slang.Cli](Slang/Rin.Slang.Cli/ReadMe.md)
- [Rin.Slang.Compiler.Tests](Slang/Rin.Slang.Compiler.Tests/ReadMe.md)
- [Rin.Slang.Compiler](Slang/Rin.Slang.Compiler/ReadMe.md)
- [Rin.Slang.Discovery](Slang/Rin.Slang.Discovery/ReadMe.md)
- [Rin.Slang.Tests](Slang/Rin.Slang.Tests/ReadMe.md)
- [Rin.Slang](Slang/Rin.Slang/ReadMe.md)

**[native](native/ReadMe.md)**
- [Fakes](native/Fakes/ReadMe.md)
- [Rin.Audio.Miniaudio.Native](native/Rin.Audio.Miniaudio.Native/ReadMe.md)
- [Rin.Graphics.Vulkan.Native](native/Rin.Graphics.Vulkan.Native/ReadMe.md)
- [Rin.Native](native/Rin.Native/ReadMe.md)
- [Rin.Slang.Native](native/Rin.Slang.Native/ReadMe.md)

**[Examples](Examples/ReadMe.md)**
- [AssetViewer](Examples/AssetViewer/ReadMe.md)
- [AudioPlayer](Examples/AudioPlayer/ReadMe.md)
- [ChatApp](Examples/ChatApp/ReadMe.md)
- [Common](Examples/Common/ReadMe.md)
- [HeadlessTest](Examples/HeadlessTest/ReadMe.md)
- [NodeGraphTest](Examples/NodeGraphTest/ReadMe.md)
- [RLTest](Examples/RLTest/ReadMe.md)
- [SceneTest](Examples/SceneTest/ReadMe.md)
- [Sponza](Examples/Sponza/ReadMe.md)
- [UiGallery](Examples/UiGallery/ReadMe.md)
- [ViewsTest](Examples/ViewsTest/ReadMe.md)

**[Experiments](Experiments/ReadMe.md)**
- [experiment.FontIcon](Experiments/experiment.FontIcon/ReadMe.md)
- [experiment.Slug](Experiments/experiment.Slug/ReadMe.md)
- [experiment.StencilAndCover](Experiments/experiment.StencilAndCover/ReadMe.md)
- [experiments.Docking](Experiments/experiments.Docking/ReadMe.md)

