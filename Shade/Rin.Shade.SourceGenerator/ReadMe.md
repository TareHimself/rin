# Rin.Shade.SourceGenerator

Roslyn incremental source generators for Rin.Shade. Not packable (`IsPackable` is false).

## Where it fits

`Rin.Shade`, `SampleStdlib` and `SampleWorld` reference it as an analyzer. `Rin.Shade.Tests` references it as a normal project and drives the generators in memory with `CSharpGeneratorDriver`.

## Generators

- `ShaderPathSourceGenerator.cs`: adds `public const string Path` to each `[Shader("...")]` class, so other attributes can use `MyShader.Path`. Reports SHADEGEN0001 (not `partial`) and SHADEGEN0002 (nested).
- `ShaderDescriptorSourceGenerator.cs`: adds a static `Descriptor` to each `partial` `[Shader]` class. A compute shader gets a `ComputeDescriptor` with its thread-group size. A graphics shader gets a `GeneratedDescriptor` record whose `Output` struct exposes the format of each `[Attachment]` color output, and which copies the expression of the most-derived `BlendState` override (the shader is never instantiated). SHADEGEN0003 is reported if that override is not an expression-bodied property.
- `ShadeExportSourceGenerator.cs`: for each `[ShadeExport]` class or struct, embeds the containing file's source text as a const string and marks the assembly with `[ShaderSources]`. `ScratchCompilationBuilder` in the transpiler reads it back.

## Test

```
dotnet test Shade/Rin.Shade.Tests/Rin.Shade.Tests.csproj
```

The generator tests are under `Rin.Shade.Tests/SourceGenerator/`.
