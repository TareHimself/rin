# Rin

## Prerequisites
- [.NET SDK 10](https://dotnet.microsoft.com/en-us/download) — pinned in `global.json` (`rollForward: latestMajor`); projects target `net10.0` (the two source-generator projects target `net8.0` for compiler compatibility)
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

## Repository layout
- `Engine/` — core engine: `Rin.Core`, `Rin.World`, `Rin.Graphics.Vulkan`, `Rin.Audio.Miniaudio`, `Rin.SourceGenerators`
- `Slang/` — managed Slang shader compiler toolchain (`Rin.Slang`, `Rin.Slang.Compiler`, `Rin.Slang.Cli`, ...)
- `native/` — native C++ modules (`Rin.Native`, `Rin.Audio.Miniaudio.Native`, `Rin.Graphics.Vulkan.Native`, `Rin.Slang.Native`)
- `Shaders/` — shared `.slang` shader sources
- `Tools/` — `Rin.Editor`, `RinLauncher`
- `Examples/` — sample/demo apps
- `Experiments/` — prototype/throwaway projects
