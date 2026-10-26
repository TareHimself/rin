# Rin.Slang.Native

Small C++ wrapper that exposes the Slang compiler to `Slang/Rin.Slang.Compiler`. Built as the shared library `Rin.Slang.Native` and packed as `TareHimself.Rin.Slang.Native`. CI packs it with `--version 1.0.1`.

```
uv run python scripts/pack_native.py Rin.Slang.Native
```

See [../ReadMe.md](../ReadMe.md) for the packing flow and the `silo sync` and `silo run` commands for a build to work on. Tests use the NativeAOT fake instead, see [../Fakes/ReadMe.md](../Fakes/ReadMe.md).

| Dependency | Version | Notes |
| --- | --- | --- |
| slang | 2025.9.2 | The prebuilt upstream release (Windows or Linux, x86_64 or aarch64), downloaded and sha256-checked by the recipe in `../recipes/slang`. Nothing of Slang is compiled. Its runtime DLLs ship in the package next to the wrapper. |

- The first build needs network access to download the Slang release.
- Sessions use row-major matrices and the SPIR-V target forces scalar buffer layout, matching how C# lays out the same structs. Change the native layout settings and the C# side together.
- Changing `src/` means `Slang/Rin.Slang.Compiler/Native.cs` and the entry points in the Slang fake must stay in sync.
- Every export that creates something returns a `SlangNativeResult` from `src/result.hpp` and writes the object through an out parameter. `CompileFailed`, `EntryPointNotFound`, `CompositionFailed` and `LinkFailed` describe the shader, with the compiler output in the diagnostics blob; the other values describe the wrapper or Slang itself. `SlangNativeResult` in `Slang/Rin.Slang.Compiler/SlangNativeException.cs` mirrors the enum.
