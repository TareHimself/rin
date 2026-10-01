# Rin.Shade.Cli

Command line wrapper over the transpiler. Assembly name is `rin-shade`.

## Where it fits

References `Rin.Shade` and `Rin.Shade.Transpiler`. It only emits Slang text, it does not run the Slang compiler.

## Usage

```
dotnet run --project Shade/Rin.Shade.Cli -- compile <input.cs>... -o <output.slang> [-r <reference.dll>]...
```

- Output defaults to the first input with a `.slang` extension.
- If the inputs declare several shaders, each is written as `<output name>.<ClassName>.slang`.
- The runtime's trusted platform assemblies and `Rin.Shade.dll` are always referenced. Add `-r` for any other assembly the shader uses.
- Exits with 1 on usage errors, on error diagnostics, or when no `[Shader]`-derived class is found.

## Open first

`Program.cs` is the whole tool.

## Gotchas

- It builds a single compilation from the files given, so it does not do the cross-assembly source embedding that `Rin.Shade.MSBuild` does through `ScratchCompilationBuilder`.
