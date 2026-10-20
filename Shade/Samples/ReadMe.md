# Samples

Three tiny assemblies that exist only to test cross-assembly shader transpilation. They are not shipped and not run.

```
dotnet build Shade/Samples/SampleGame
```

| Project | Contents | References |
| --- | --- | --- |
| `SampleStdlib` | `Sd` (`[ShadeExport]`) with `SdCircle` and `Dot2` over local `[SlangExpression]` intrinsics, and `MathHelpers`, a plain class that is not exported. | Rin.Shade, the generator as an analyzer |
| `SampleWorld` | `BaseMeshShader` (`[ShadeExport]`), an abstract shader whose `[Compute]` method calls `Sd.SdCircle`. | Rin.Shade, SampleStdlib, the generator as an analyzer |
| `SampleGame` | `DerivedMeshShader`, a `[Shader]` that overrides `Compute` and calls into both. | Rin.Shade, SampleWorld |

The chain is `SampleGame -> SampleWorld -> SampleStdlib`. The base class body and the `Sd` helpers exist only as source embedded in the referenced DLLs by `Rin.Shade.SourceGenerator`, which is the situation a real downstream shader assembly is in.

`Rin.Shade.Tests/Emitter/CrossAssemblyTests.cs` loads `SampleStdlib.dll` and `SampleWorld.dll` from `<project>/bin/<configuration>/net10.0/`, reads `SampleGame/DerivedMeshShader.cs` as text, and checks that the emitted Slang contains the base push constants and the transitively reached `sdCircle`. `Rin.Shade.Tests.csproj` builds SampleStdlib and SampleWorld first; SampleGame is only read as source.
