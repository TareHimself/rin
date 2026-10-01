# Rin.Slang.Native

Small C++ wrapper that exposes the Slang compiler to C#. Builds `Rin.Slang.Native` as a shared library and packs it as `TareHimself.Rin.Slang.Native`.

## Where it fits

- `Slang/Rin.Slang.Compiler` references the `TareHimself.Rin.Slang.Native` package and calls it through `Native.cs`.
- Tests use the NativeAOT fake instead, see [../Fakes/ReadMe.md](../Fakes/ReadMe.md).

## How it is built

`CMakeLists.txt` does not build Slang. The `FindSlang` macro downloads a prebuilt Slang release zip (version `2025.9.2`, for the current OS and x86_64 or aarch64) into the build directory, links its library, and copies its runtime binaries next to the built library so they end up in the package. Only the wrapper in `src/` (`slang.cpp`, `slang.hpp`, `macro.hpp`) is compiled. `conanfile.py` has no `requires`.

## Build and pack

From the repo root:

```
uv run task pack-slang-native
```

Or from this folder, `uv run task build` (Release) or `uv run task buildd` (Debug). CI runs `conan install . --build=missing -s build_type=Release -s compiler.cppstd=20` and then the `Rin-Release` CMake preset. See [../ReadMe.md](../ReadMe.md) for the packing flow.

## Gotchas

- The first build needs network access to download the Slang release.
- The C++ standard is 20. In CI the Conan profile defaults to an older standard, so `-s compiler.cppstd=20` is passed explicitly.
- Changing `src/` means the C# declarations in `Slang/Rin.Slang.Compiler/Native.cs` and the entry points in the Slang fake must stay in sync.
