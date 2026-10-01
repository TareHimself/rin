# Shade

Rin.Shade lets you write shaders in C#. A Roslyn-based transpiler turns the C# shader classes into Slang source, and the existing Slang toolchain (`Slang/`) compiles that to a shader package embedded in the consuming assembly.

## Projects

| Project | What it is |
| --- | --- |
| [Rin.Shade](Rin.Shade/ReadMe.md) | The authoring API: `Shader` base class, attributes, intrinsics, `BufferRef<T>`, descriptors. |
| [Rin.Shade.Transpiler](Rin.Shade.Transpiler/ReadMe.md) | The Roslyn transpiler from C# shader classes to Slang text (`ShadeEmitter`). |
| [Rin.Shade.SourceGenerator](Rin.Shade.SourceGenerator/ReadMe.md) | Roslyn source generators: `Path` constant, `Descriptor`, and exported shader source. |
| [Rin.Shade.MSBuild](Rin.Shade.MSBuild/ReadMe.md) | The `CompileRinShadeShaders` MSBuild task used by `msbuild/RinShade.targets`. |
| [Rin.Shade.Cli](Rin.Shade.Cli/ReadMe.md) | `rin-shade compile`, a command line wrapper over the transpiler. |
| [Rin.Shade.Tests](Rin.Shade.Tests/ReadMe.md) | Snapshot and unit tests for the transpiler and generators (no native Slang needed). |
| [Rin.Shade.CpuTests](Rin.Shade.CpuTests/ReadMe.md) | Runs transpiled shaders on the CPU through the real Slang. |
| [Samples](Samples/ReadMe.md) | SampleStdlib, SampleWorld and SampleGame, three chained assemblies used by cross-assembly tests. |

## How a shader goes from C# to a compiled shader

1. A shader is a class that derives from `Rin.Shade.Shader` and carries `[Shader("Shaders/.../name.slang")]`. Entry points are marked `[Compute(x, y, z)]`, or `[Vertex]` and `[Fragment]`.
2. While the C# project compiles, `Rin.Shade.SourceGenerator` adds a `Path` constant and a `Descriptor` (attachment formats, blend state, thread-group size) to each `partial` shader class. Types marked `[ShadeExport]` also get their source text embedded as a constant so downstream assemblies can transpile calls into them.
3. `Rin.Shade.Transpiler` (`ShadeEmitter.Emit`) walks the project's Roslyn compilation, lowers each `[Shader]` class to Slang text, and reports diagnostics for C# it cannot translate.
4. `Rin.Shade.MSBuild` runs that step inside the build (the `CompileShadeShaders` target in `msbuild/RinShade.targets`), writes the Slang to a scratch location under `obj`, compiles it with `Rin.Slang.Compiler`, and embeds the resulting `.crsh` file as an `EmbeddedResource`. No `.slang` file is written into the tracked tree.

A shader from `Engine/Rin.Core/Views/Graphics/Shaders/BlurShader.cs` (start of the class):

```csharp
[Shader("Shaders/Rin/Core/Views/blur.slang")]
public partial class BlurShader : ViewShader<BlurShader.FragmentIn>
{
    public struct PushConstants
    {
        public BufferRef<BlurData> Data;
        public int IsHorizontal;
    }

    [Push] protected PushConstants Push;
    protected static BindlessData Bindless;

    [Vertex]
    public VertexOut Vertex(VertexIn input) { ... }
}
```

## Build integration

A project that contains Shade shaders sets `RinShadeDiscoverPrefix` (the path prefix its `[Shader]` paths start with) and `RinShadeOutputSubpath`, then imports `msbuild/RinShade.targets`. `Engine/Rin.Core/Rin.Core.csproj` is an example. See [Rin.Shade.MSBuild](Rin.Shade.MSBuild/ReadMe.md) for the properties and gotchas.

## Dependencies

```
Rin.Shade  <-- Rin.Shade.Transpiler  <-- Rin.Shade.Cli
   |                  ^
   |                  +-- Rin.Shade.MSBuild (also Slang/Rin.Slang.Compiler)
   +-- Rin.Shade.SourceGenerator (analyzer reference, not a runtime dependency)
Rin.Shade.Tests    -> Rin.Shade, Transpiler, SourceGenerator; builds SampleStdlib and SampleWorld
Rin.Shade.CpuTests -> Rin.Shade, Transpiler, Slang/Rin.Slang.Compiler
Samples            -> Rin.Shade (SampleGame -> SampleWorld -> SampleStdlib)
```
