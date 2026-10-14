# Rin.Native

Core native helpers for the engine. Builds `Rin.Native` as a shared library and packs it as `TareHimself.Rin.Native`.

## Where it fits

- `Engine/Rin.Core` references the `TareHimself.Rin.Native` package.
- `Engine/Rin.GLTF.Tests` uses the NativeAOT fake of it, see [../Fakes/ReadMe.md](../Fakes/ReadMe.md).

## Start here

- `CMakeLists.txt`: links `webmdx::webmdx` and `msdfgen::msdfgen-core`.
- `silopkg.py`: the silo recipe. Depends on `webmdx@1.0.0` and on `msdfgen@1.12` without its PNG, SVG and command line parts. Both are static libraries, so the package holds `Rin.Native.dll` alone.
- `src/`: `memory.*`, `platform.*`, `sdf.*`, `video.*`, `flags.hpp`, `macro.hpp`.
- `project.csproj`: shell project used only by `dotnet pack`.

## Build and pack

From the repo root:

```
uv run python scripts/pack_native.py Rin.Native
```

For a build to work on, `uv run silo sync native/Rin.Native` builds the dependencies and writes the `Rin-Debug` and `Rin-Release` CMake presets, which an IDE can open or `uv run silo run -C native/Rin.Native -- cmake --preset Rin-Debug` can configure from this folder. See [../ReadMe.md](../ReadMe.md) for the packing flow.
