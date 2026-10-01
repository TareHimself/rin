# Fakes

C# projects that are published with NativeAOT into real native libraries, standing in for the C++ modules in tests. They let tests that call into native code run without Conan, CMake or the Vulkan SDK.

| Project | Replaces | Used by |
| --- | --- | --- |
| `Rin.Native.Fake` | `Rin.Native` | `Engine/Rin.GLTF.Tests` |
| `Rin.Slang.Native.Fake` | `Rin.Slang.Native` | `Slang/Rin.Slang.Compiler.Tests` |

Both projects target `net10.0` with `PublishAot`, `AllowUnsafeBlocks` and `InvariantGlobalization`, and are not packable. Each has a single `NativeFake.cs` exporting functions with `[UnmanagedCallersOnly(EntryPoint = "...")]` (for example `memoryAllocate` in the `Rin.Native` fake and `slangSessionBuilderNew` in the Slang fake). They implement only what the tests need. The Slang fake, for instance, is enough for `ShaderCompiler.TryCompile` to succeed on compute-only shaders and does not run real Slang.

## Publish

```
python scripts/publish_native_fakes.py win-x64
```

The script runs `dotnet publish -c Release -r <rid> --self-contained` for every `native/Fakes/*/*.csproj`. The RID argument defaults to `win-x64`.

## How tests find them

Each test project has `NativeFakeLocator.cs` (walks up to `rin.sln` and finds `<name>.dll` under a `publish` folder in `native/Fakes/<name>`) and `NativeFakeSetup.cs` (a `NativeLibrary.SetDllImportResolver` that loads the fake when the managed code asks for the real library name). If the fake is not published, the locator throws and says to run `scripts/publish_native_fakes.py`.

See [../ReadMe.md](../ReadMe.md) for how fakes differ from real and stub packages.
