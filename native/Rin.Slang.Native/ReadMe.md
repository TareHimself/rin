# Rin.Slang.Native

Small C++ wrapper that exposes the Slang compiler to C#. Builds `Rin.Slang.Native` as a shared library and packs it as `TareHimself.Rin.Slang.Native`.

## Where it fits

- `Slang/Rin.Slang.Compiler` references the `TareHimself.Rin.Slang.Native` package and calls it through `Native.cs`.
- Tests use the NativeAOT fake instead, see [../Fakes/ReadMe.md](../Fakes/ReadMe.md).

## How it is built

Nothing here builds Slang. The recipe in `../recipes/slang/2025.9.2` downloads upstream's prebuilt release zip (for Windows or Linux, x86_64 or aarch64), checks its sha256 and stores its headers and libraries as the silo package `slang`. `silopkg.py` depends on `slang@2025.9.2`, and `CMakeLists.txt` links `slang::slang` and installs Slang's runtime libraries next to the built library so they end up in the package. Only the wrapper in `src/` (`slang.cpp`, `slang.hpp`, `macro.hpp`) is compiled.

## Build and pack

From the repo root:

```
uv run python scripts/pack_native.py Rin.Slang.Native
```

For a build to work on, `uv run silo sync native/Rin.Slang.Native` builds the dependencies and writes the `Rin-Debug` and `Rin-Release` CMake presets, which an IDE can open or `uv run silo run -C native/Rin.Slang.Native -- cmake --preset Rin-Debug` can configure from this folder. See [../ReadMe.md](../ReadMe.md) for the packing flow.

CI runs the same `pack_native.py` command with `--version 1.0.1`.

## Session settings

Every session builder creates its own Slang global session, which the session then owns, because Slang's global session is not thread-safe (`slang.h`, "The global session is currently not thread-safe"). Sessions built on different threads can therefore compile at the same time, at the cost of loading Slang's core module once per session (about 450 ms).

The SPIR-V target is created with `forceGLSLScalarBufferLayout`, so every block follows scalar layout, and the session uses row-major matrices. Both match what C# lays out for the same structs. `ScalarLayoutTests` in `Shade/Rin.Shade.CpuTests` checks the first against the compiled SPIR-V.

## Gotchas

- The first build needs network access to download the Slang release.
- The C++ standard is 20, set in `CMakeLists.txt` after `project()` so it replaces the standard of the silo profile.
- Changing `src/` means the C# declarations in `Slang/Rin.Slang.Compiler/Native.cs` and the entry points in the Slang fake must stay in sync.
