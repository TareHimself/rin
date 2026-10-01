# Rin.Shade.Tests

NUnit tests for the transpiler and the source generators. Does not need the native Slang library, which is why the CPU-execution tests live in `Rin.Shade.CpuTests`.

## Where it fits

References `Rin.Shade`, `Rin.Shade.Transpiler` and `Rin.Shade.SourceGenerator` (as a plain reference, the generators are driven in memory). It also references `Samples/SampleStdlib` and `Samples/SampleWorld` with `ReferenceOutputAssembly="false"` so they are built first (see the Samples ReadMe).

## Layout

- `Emitter/`: transpiler tests, one file per language area (control flow, inheritance, unions, intrinsics, diagnostics, ...).
- `SourceGenerator/`: tests for the descriptor and `[ShadeExport]` generators.
- `Fixtures/`: shader source used by the tests.
- `TestDoubles.cs`: shared test helpers (also linked into `Rin.Shade.CpuTests`).
- `Snapshot.cs`: golden-file helper.

## Run

```
dotnet test Shade/Rin.Shade.Tests/Rin.Shade.Tests.csproj
```

## Snapshots

`Snapshot.Verify` compares emitted Slang with `Snapshots/<TestFile>.<TestName>[.<suffix>].slang` next to the test file. On a mismatch it writes a `.received.slang` file beside it and fails with the first differing line. To accept new output, run the tests with the environment variable `RIN_SHADE_SNAPSHOTS=update`, then review the change with `git diff`.

## Gotchas

- `CrossAssemblyTests` loads the sample DLLs from `Shade/Samples/<project>/bin/<configuration>/net10.0/` and fails with a "build Shade/Samples/SampleGame first" message if they are missing.
