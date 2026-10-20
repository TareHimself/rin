# Rin.Shade.SourceGenerator

Two Roslyn incremental generators that run in every project that references Rin.Shade, because Rin.Shade references this project as an analyzer.

| Generator | Adds | Diagnostics |
| --- | --- | --- |
| `ShaderDescriptorSourceGenerator` | A static `Descriptor` on each `partial` `[Shader]` class, naming its assembly and full class name (also the name of its `.crsh`): a `ComputeDescriptor` with the thread-group size, or a `GeneratedDescriptor` with each `[Attachment]` format under `Output`, the expression of the `BlendState` override, and depth and stencil use. | SHADEGEN0001 not `partial`, SHADEGEN0002 nested, SHADEGEN0003 `BlendState` override is not an expression-bodied property. |
| `ShadeExportSourceGenerator` | One const string per file that holds a `[ShadeExport]` type, plus `[assembly: ShaderSources]`, which the transpiler's `ScratchCompilationBuilder` reads back from the compiled DLL. | |

```
dotnet test Shade/Rin.Shade.Tests/Rin.Shade.Tests.csproj
```

The tests under `Rin.Shade.Tests/SourceGenerator/` drive the generators in memory with `CSharpGeneratorDriver`.

References: none. Packages: Microsoft.CodeAnalysis.CSharp, Microsoft.CodeAnalysis.CSharp.Workspaces, Microsoft.CodeAnalysis.Analyzers. Not packable.
