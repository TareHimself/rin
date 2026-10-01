# Samples

Three tiny assemblies that exist only to test cross-assembly shader transpilation. They are not shipped and not run.

| Project | Contents | References |
| --- | --- | --- |
| `SampleStdlib` | `Sd` (marked `[ShadeExport]`) with `SdCircle` and `Dot2`, plus local `[SlangExpression]` intrinsics. `NotEmbedded.cs` holds `MathHelpers`, a plain helper class. | `Rin.Shade`, `Rin.Shade.SourceGenerator` (analyzer) |
| `SampleWorld` | `BaseMeshShader` (`[ShadeExport]`, abstract shader with push constants and a `[Compute]` method calling `Sd.SdCircle`). | `Rin.Shade`, `SampleStdlib`, source generator (analyzer) |
| `SampleGame` | `DerivedMeshShader`, a `[Shader]` that overrides `Compute` and calls into both `SampleWorld` and `SampleStdlib`. | `Rin.Shade`, `SampleWorld` |

The chain is `SampleGame -> SampleWorld -> SampleStdlib`. The base class body and the `Sd` helpers exist as source only inside the referenced DLLs, embedded by `Rin.Shade.SourceGenerator`, which is the situation a real downstream shader assembly is in.

## Why they exist

`Rin.Shade.Tests/Emitter/CrossAssemblyTests.cs` loads `SampleStdlib.dll` and `SampleWorld.dll` from `<project>/bin/<configuration>/net10.0/`, reads `SampleGame/DerivedMeshShader.cs` as text, and runs `ScratchCompilationBuilder` and `ShadeEmitter` on it. It then checks that the emitted Slang contains the base push-constants struct and the transitively reached `sdCircle`. `Rin.Shade.Tests.csproj` references SampleStdlib and SampleWorld (without referencing their output) so they build first.

The test project does not reference SampleGame, only reads its source file. Build the samples first if the DLLs are missing:

```
dotnet build Shade/Samples/SampleGame
```
