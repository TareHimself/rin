# Rin

## Prerequisites
- [.NET SDK 10](https://dotnet.microsoft.com/en-us/download) — pinned in `global.json` (`rollForward: latestMajor`); every project targets `net10.0` (set in `Directory.Build.props`)
- [Vulkan SDK](https://www.lunarg.com/vulkan-sdk/)
- [CMake](https://cmake.org/)
- [Python 3](https://www.python.org/)
- [Conan](https://conan.io/) (`pip install conan`) — used to fetch/build the native C++ dependencies
- [Task](https://taskfile.dev/installation/) — runs the native build/pack pipeline defined in `Taskfile.yml`

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
