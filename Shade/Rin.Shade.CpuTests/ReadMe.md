# Rin.Shade.CpuTests

NUnit tests that transpile a shader, compile it with Slang's host-callable (CPU) target and run it in-process, then compare the result with `System.Numerics`. The main check is that C# and Slang agree on matrix convention (row-major storage, row-vector math).

## Where it fits

References `Rin.Shade`, `Rin.Shade.Transpiler` and `Slang/Rin.Slang.Compiler`, and links `Rin.Shade.Tests/TestDoubles.cs`. It is a separate project so `Rin.Shade.Tests` stays free of native dependencies.

## Requirements

The real native Slang library, not the stub packages. In CI this runs in the `test-real-slang` job, which depends on the native Slang build job.

## Open first

- `MatrixConventionTests.cs`
- `MatrixInverseTests.cs`

## Run

```
dotnet test Shade/Rin.Shade.CpuTests/Rin.Shade.CpuTests.csproj
```
