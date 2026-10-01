# Rin.Slang.Compiler.Tests

NUnit tests for `ShaderCompiler`. Targets `net10.0`, not packable, references `Rin.Slang.Compiler`.

## Start here

- `ShaderCompilerTests.cs`: the tests.
- `NativeFakeSetup.cs`: redirects the `Rin.Slang.Native` DllImport (via `NativeLibrary.SetDllImportResolver`) to the published fake library.
- `NativeFakeLocator.cs`: walks up to `rin.sln`, then finds the published `Rin.Slang.Native.Fake.dll` under `native/Fakes/Rin.Slang.Native.Fake`.

## Run

Publish the fake first, then test:

```
python scripts/publish_native_fakes.py win-x64
dotnet test Slang/Rin.Slang.Compiler.Tests/Rin.Slang.Compiler.Tests.csproj
```

CI does the same, with `-p:RinShadeSkipCompile=true` on the test command.

## Gotchas

- Without the published fake, the locator throws "Rin.Slang.Native.Fake.dll not published".
- The fake returns just enough for compute-only shaders to compile. It does not run real Slang, so it cannot check real compiler output. See [native/Fakes/ReadMe.md](../../native/Fakes/ReadMe.md).
