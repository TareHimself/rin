# Rin.Shade.CpuTests

NUnit tests that transpile a shader, compile it with Slang's host-callable (CPU) target, run it in-process and compare the numbers with `System.Numerics`. They need the real native Slang library, so CI runs them in the `test-real-slang` job after the native build.

```
dotnet test Shade/Rin.Shade.CpuTests/Rin.Shade.CpuTests.csproj
```

| Test | Proves |
| --- | --- |
| `MatrixConventionTests` | Chained `Vector4.Transform` and `model * view * projection` give the same result as `System.Numerics`, which pins row-major storage and row-vector math. |
| `MatrixInverseTests` | The C# inverse matches `System.Numerics`. |
| `WithExpressionTests` | `with` on a record struct and on a property with a setter body compiles. |
| `LanguageFeatureTests` | Static readonly constants, switch expressions, extension members and generic methods against hand-computed values. |
| `ScalarLayoutTests` | Hand-written Slang compiled to SPIR-V gives pointer and `ParameterBlock` structs scalar offsets. |

References: Rin.Shade, Rin.Shade.Transpiler, Slang/Rin.Slang.Compiler, plus `Rin.Shade.Tests/TestDoubles.cs` as a linked file. Packages: NUnit, NUnit3TestAdapter, NUnit.Analyzers, Microsoft.NET.Test.Sdk, Microsoft.CodeAnalysis.CSharp.
