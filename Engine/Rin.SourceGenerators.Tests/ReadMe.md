# Rin.SourceGenerators.Tests

xUnit tests for the generators in [Rin.SourceGenerators](../Rin.SourceGenerators/ReadMe.md), one test file per generator, using `Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.XUnit`.

```
dotnet test Engine/Rin.SourceGenerators.Tests/Rin.SourceGenerators.Tests.csproj -p:RinShadeSkipCompile=true
```

CI passes `-p:RinShadeSkipCompile=true` for this project.

References: Rin.Core, Rin.World, Rin.SourceGenerators (all as normal project references). The component module tests compile the generated code and load it, so they check real field offsets.
