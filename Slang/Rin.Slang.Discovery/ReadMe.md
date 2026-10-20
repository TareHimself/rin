# Rin.Slang.Discovery

Finds which shaders C# code references by parsing the source text with Roslyn: `[GraphicsShader("...")]` and `[ComputeShader("...")]` attributes and `MakeGraphics("...")` and `MakeCompute("...")` calls. Packed as `TareHimself.Rin.Slang.Discovery`.

```
dotnet build Slang/Rin.Slang.Discovery/Rin.Slang.Discovery.csproj
```

Dependencies: `Microsoft.CodeAnalysis.CSharp` only. Used by the `discover` and `compile-referenced` commands of `Rin.Slang.Cli`. It has no test project.

- It never compiles the C# code, so it works across project boundaries and does not depend on build order, which a source generator (scoped to its own project) could not do.
