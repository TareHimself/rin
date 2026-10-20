# Rin.Shade.Tests

NUnit tests for the transpiler and the source generators. They need no native Slang; the tests that run shaders live in `Rin.Shade.CpuTests`.

```
dotnet test Shade/Rin.Shade.Tests/Rin.Shade.Tests.csproj
```

| Folder | Covers |
| --- | --- |
| `Emitter/` | The transpiler, one file per language area, with the golden Slang under `Emitter/Snapshots/`. |
| `SourceGenerator/` | The descriptor and `[ShadeExport]` generators, driven in memory. |
| `Fixtures/` | Shader sources the tests compile. |
| root | `SlangDiagnosticTests`, the `Snapshot` helper and `TestDoubles` (also linked into `Rin.Shade.CpuTests`). |

`Snapshot.Verify` compares emitted Slang with `Snapshots/<TestFile>.<TestName>.slang` next to the test. A mismatch writes a `.received.slang` file beside it and fails. To accept new output, run the tests with `RIN_SHADE_SNAPSHOTS=update` and review the diff.

Gotcha: `CrossAssemblyTests` loads the sample DLLs from `Samples/<project>/bin/<configuration>/net10.0/`, so the csproj builds `SampleStdlib` and `SampleWorld` first without referencing their output.

References: Rin.Shade, Rin.Shade.Transpiler, Rin.Shade.SourceGenerator (a plain reference, not an analyzer), Samples/SampleStdlib and Samples/SampleWorld (build order only). Packages: NUnit, NUnit3TestAdapter, NUnit.Analyzers, Microsoft.NET.Test.Sdk, Microsoft.CodeAnalysis.CSharp, coverlet.collector.
