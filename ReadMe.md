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
