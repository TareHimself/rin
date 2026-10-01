# Rin.SourceGenerators.Tests

xUnit tests for the generators in [Rin.SourceGenerators](../Rin.SourceGenerators/ReadMe.md), using `Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.XUnit`.

## Where it fits

- References: Rin.Core and Rin.SourceGenerators (both as normal project references).

## Start here

- `AudioEffectGeneratorTest.cs`, `GraphicsShaderGeneratorTests.cs`, `ProviderResolvedGeneratorTests.cs`: tests per generator.
- `SourceGeneratorWithAttributesTests.cs`, `SourceGeneratorWithAdditionalFilesTests.cs`: shared test helpers.
- `Utils/TestAdditionalFile.cs`: additional-file helper.

## Run

```
dotnet test Engine/Rin.SourceGenerators.Tests/Rin.SourceGenerators.Tests.csproj -p:RinShadeSkipCompile=true
```

CI passes `-p:RinShadeSkipCompile=true` for this project.
