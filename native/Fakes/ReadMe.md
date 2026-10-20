# Fakes

C# projects published with NativeAOT into real native libraries that export the same entry points as the C++ modules, so tests that call into native code run without silo or a C++ toolchain. Each fake implements only what its tests need; the Slang fake does not run real Slang.

```
uv run python scripts/publish_native_fakes.py win-x64
```

The RID defaults to `win-x64`. The script runs `dotnet publish -c Release -r <rid> --self-contained` for every project here.

| Project | Replaces | Used by |
| --- | --- | --- |
| `Rin.Native.Fake` | `Rin.Native` | `Engine/Rin.GLTF.Tests` |
| `Rin.Slang.Native.Fake` | `Rin.Slang.Native` | `Slang/Rin.Slang.Compiler.Tests` |

Each test project finds the published `<name>.dll` under `native/Fakes/<name>` and loads it with a `NativeLibrary.SetDllImportResolver` in place of the real library. Fakes are not packages and never go into `.feed/`. See [../ReadMe.md](../ReadMe.md) for how fakes differ from real and stub packages.
