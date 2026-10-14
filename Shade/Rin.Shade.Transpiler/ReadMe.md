# Rin.Shade.Transpiler

Roslyn-based transpiler from C# shader classes to Slang source text. Build-time tooling, so it favours clarity over allocation.

## Where it fits

- References `Rin.Shade` and Roslyn (`Microsoft.CodeAnalysis.CSharp` and `.Workspaces`).
- Used by `Rin.Shade.MSBuild` (the real build), `Rin.Shade.Cli`, `Rin.Shade.Tests` and `Rin.Shade.CpuTests`.

## Open first

- `ShadeEmitter.cs`: entry point. `ShadeEmitter.Emit(compilation)` finds every class that derives from `Shader` and has `[Shader(...)]`, and returns Slang text and a `SlangSourceMap` per class name plus diagnostics.
- `SlangWriter.cs`, `SlangSourceMap.cs`: the text builder records the C# `Location` in force when each line was written (set per statement by `BodyLowering` and per signature by `FunctionLowering`), and the map answers which C# location a 1-based Slang line came from.
- `SlangDiagnostic.cs`: parses the Slang compiler's `path(line): error code: message` output, for the MSBuild task to map through the source map.
- `ScratchCompilationBuilder.cs`: builds one compilation from the project's own sources (as `LocalSource` path and text pairs, so locations name the file) plus the source text that `[ShaderSources]`-marked referenced assemblies embed (see `GeneratedSourceReader.cs`). This is how a shader can derive from a base class or call a helper defined in another assembly.
- `ShaderLowering.cs`, `BodyLowering.cs`, `FunctionLowering.cs`, `StructLowering.cs`: lowering of a shader, method bodies, functions and structs.
- `Diagnostics.cs`: the diagnostics reported for unsupported C#.

## Build and test

```
dotnet build Shade/Rin.Shade.Transpiler/Rin.Shade.Transpiler.csproj
dotnet test Shade/Rin.Shade.Tests/Rin.Shade.Tests.csproj
```

## Gotchas

- `Rin.Shade.MSBuild` is rebuilt before every shader compile, so changes here reach the real build on the next build (see its ReadMe for the node reuse gotcha).
- `ShadeEmitter.Emit(compilation, localTrees)` only treats classes in `localTrees` as entry points. Trees pulled in from other assemblies exist only so their bodies can be walked.
