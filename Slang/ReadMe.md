# Slang

Managed toolchain around the Slang shader compiler. It compiles `.slang` files into the shader package format the engine loads, and finds which shaders the C# code references.

| Project | What it is |
| --- | --- |
| [Rin.Slang](Rin.Slang/ReadMe.md) | Shader package format (compiled stages, manifest, reflection data, reader and writer). No native dependency. |
| [Rin.Slang.Compiler](Rin.Slang.Compiler/ReadMe.md) | `ShaderCompiler` and the P/Invoke layer over `Rin.Slang.Native`. |
| [Rin.Slang.Discovery](Rin.Slang.Discovery/ReadMe.md) | Roslyn-based scanner that finds shader keys referenced in `.cs` files. |
| [Rin.Slang.Cli](Rin.Slang.Cli/ReadMe.md) | `rin-slang` command line tool (`compile`, `discover`, `compile-referenced`). |
| [Rin.Slang.Tests](Rin.Slang.Tests/ReadMe.md) | Tests for the package format. |
| [Rin.Slang.Compiler.Tests](Rin.Slang.Compiler.Tests/ReadMe.md) | Tests for `ShaderCompiler`, run against the NativeAOT Slang fake. |

## How the pieces connect

- `Rin.Slang` holds the data types. `Engine/Rin.Graphics.Vulkan` references it to read compiled shaders.
- `Rin.Slang.Compiler` produces those types. It calls into the native `Rin.Slang.Native` library (the `TareHimself.Rin.Slang.Native` package, built from [native/Rin.Slang.Native](../native/Rin.Slang.Native/ReadMe.md)).
- `Rin.Slang.Cli` ties `Rin.Slang.Compiler` and `Rin.Slang.Discovery` together: discovery finds the shader keys, the compiler builds them.
- `Shade/Rin.Shade.MSBuild` and `Shade/Rin.Shade.CpuTests` also reference `Rin.Slang.Compiler`.

Compiling real shaders needs the real Slang native package. Tests that only need the compiler to run use the fake, see [native/ReadMe.md](../native/ReadMe.md).
