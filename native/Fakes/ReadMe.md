# Fakes

C# projects published with NativeAOT into real native libraries that export the same entry points as the C++ modules, so tests that call into native code run without silo or a C++ toolchain. Each fake implements only what its tests need; the Slang fake does not run real Slang.

```
uv run python scripts/publish_native_fakes.py win-x64
```

The RID defaults to `win-x64`. The script runs `dotnet publish -c Release -r <rid> --self-contained` for every project here.

| Project | Replaces | Used by |
| --- | --- | --- |
| `Rin.Slang.Native.Fake` | `Rin.Slang.Native` | `Slang/Rin.Slang.Compiler.Tests` |
| `Rin.Graphics.Vulkan.Native.Fake` | `Rin.Graphics.Vulkan.Native` | `Engine/Rin.Graphics.Vulkan.Tests` |

Each test project finds the published `<name>.dll` under `native/Fakes/<name>` and loads it with a `NativeLibrary.SetDllImportResolver` in place of the real library. Fakes are not packages and never go into `.feed/`. See [../ReadMe.md](../ReadMe.md) for how fakes differ from real and stub packages.

A fake mirrors the result-code shape of the export it replaces: an export that returns a module enum returns `0` (`Ok`) and writes its object through the out parameter. The Vulkan fake also exports `fake*` controls (`fakeFailNext`, `fakeSetEventText`, `fakeDragEnter` and so on) that its tests use to script the next result code, the text behind a text event and a window's drop callbacks. `publish_native_fakes.py` needs `vswhere.exe` on `PATH` (the Visual Studio installer folder), which NativeAOT uses to find the linker.
