# Rin.Slang.Compiler.Tests

NUnit tests for `ShaderCompiler`, run against the NativeAOT fake of `Rin.Slang.Native` instead of the real library. Targets `net10.0`, not packable.

Publish the fake first, then test:

```
uv run python scripts/publish_native_fakes.py win-x64
dotnet test Slang/Rin.Slang.Compiler.Tests/Rin.Slang.Compiler.Tests.csproj
```

CI does the same, with `-p:RinShadeSkipCompile=true` on the test command.

References: `Rin.Slang.Compiler`.

- The fake returns just enough for compute-only shaders to compile and does not run real Slang, so these tests cannot check real compiler output. Without the published fake the tests fail with "Rin.Slang.Native.Fake.dll not published". See [native/Fakes/ReadMe.md](../../native/Fakes/ReadMe.md).
