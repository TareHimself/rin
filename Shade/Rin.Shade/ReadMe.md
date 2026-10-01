# Rin.Shade

The C# authoring API for shaders. A shader is a class deriving from `Shader` whose methods are written in a C# subset that `Rin.Shade.Transpiler` lowers to Slang. Most members here are plain C# stubs or `[SlangExpression]` bindings.

## Where it fits

- References `Rin.Shade.SourceGenerator` as an analyzer only (`OutputItemType="Analyzer"`), so every project using Rin.Shade gets the generators.
- Referenced by the transpiler, the CLI, both test projects and the samples.

## Open first

- `Attributes.cs`: `[Shader]`, `[Compute]`, `[Vertex]`, `[Fragment]`, `[Attachment]`, `[Depth]`, `[Stencil]`, `[Push]`, `[BindingGroup]`, `[BindlessBlock]`, `[ShadeExport]`, `[SlangExpression]`, `[SlangStatement]`, and the semantic attributes (`[Position]`, `[VertexId]`, `[Target]`, ...).
- `Shader.cs`, `Shader.Math.cs`, `Shader.Intrinsics.cs`: the base class and its math and intrinsic members.
- `BufferRef.cs`, `Resources.cs`: buffer and resource types usable in push constants.
- `Descriptors.cs`, `BlendState.cs`: the descriptor types that the generated `Descriptor` property instantiates.

## Build

```
dotnet build Shade/Rin.Shade/Rin.Shade.csproj
```

## Gotchas

- A `[Shader]` class must be `partial` and top-level (SHADEGEN0001 and SHADEGEN0002 otherwise), because the generators add members to it.
- `[ShaderStruct]` currently has no effect on emission (see its doc comment); the transpiler lowers any struct a shader reaches.
- `[SlangExpression]` templates use `@0`, `@name`, `@T0` and `@this` placeholders; an unknown placeholder is a diagnostic.
