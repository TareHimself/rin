# Rin.Slang.Discovery

Finds which shaders C# code references, without compiling the C# code. Packed as `TareHimself.Rin.Slang.Discovery`. Its only dependency is `Microsoft.CodeAnalysis.CSharp`.

## Where it fits

- Used by `Rin.Slang.Cli` (the `discover` and `compile-referenced` commands).

## Start here

- `ShaderReferenceScanner.cs`: `ScanDirectory(root)` and `Scan(csFiles)` return the set of shader content keys found in `[GraphicsShader("...")]` and `[ComputeShader("...")]` attributes and in `MakeGraphics("...")` and `MakeCompute("...")` calls.

## Notes

- It parses source text only, so it works across project boundaries and does not depend on build order. A Roslyn generator only sees its own project, which would miss shaders referenced from another project.

## Build

```
dotnet build Slang/Rin.Slang.Discovery/Rin.Slang.Discovery.csproj
```

There is no test project for it under `Slang/`.
