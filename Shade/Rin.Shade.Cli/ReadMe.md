# Rin.Shade.Cli

`rin-shade compile` runs the transpiler on C# files and writes the Slang. It does not run the Slang compiler.

```
dotnet run --project Shade/Rin.Shade.Cli -- compile <input.cs>... -o <output.slang> [-r <reference.dll>]...
```

| Option | Meaning |
| --- | --- |
| `<input.cs>...` | The files, compiled together as one compilation. |
| `-o`, `--output` | The output path. Default: the first input with a `.slang` extension. Several shaders are written as `<output name>.<ClassName>.slang`. |
| `-r`, `--reference` | An extra assembly. The runtime's assemblies and `Rin.Shade.dll` are always referenced. |

Diagnostics go to stderr. The exit code is 1 on a usage error, an error diagnostic, or when no `[Shader]` class is found.

Gotcha: only the files given are compiled, so it does not pull `[ShadeExport]` source out of referenced DLLs the way the MSBuild task does.

References: Rin.Shade, Rin.Shade.Transpiler. Packages: Microsoft.CodeAnalysis.CSharp.
