# Rin Source Generators

Roslyn incremental source generators, referenced by Rin.Core and Rin.World as an analyzer (`OutputItemType="Analyzer"`, `ReferenceOutputAssembly="false"`).

## Where it fits

- References only Roslyn packages (`Microsoft.CodeAnalysis.CSharp`, `...CSharp.Workspaces`, `...Analyzers`). It is not packable.
- Referenced by: Rin.Core and Rin.World (as analyzer), and Rin.SourceGenerators.Tests.

## Content
### Rin.SourceGenerators
Implementations of the source generators.
**You must build this project to see the result (generated code) in the IDE.**

- [AudioEffectGenerator.cs](AudioEffectGenerator.cs): types marked with `Rin.Core.Audio.Effects.AudioEffectAttribute`.
- [GraphicsShaderGenerator.cs](GraphicsShaderGenerator.cs): properties marked with the shader attributes in `Rin.Core.Graphics.Shaders` (and the `Rin.Shade` shader attribute).
- [ProviderResolvedGenerator.cs](ProviderResolvedGenerator.cs): properties marked with `Rin.Core.Shared.Providers.ResolvedAttribute`.
- `Diagnostics.cs`, `GeneratorUtils.cs`, `SourceBuilder.cs`: shared diagnostics and helpers.

### Rin.SourceGenerators.Tests
Unit tests for the source generators, in [Rin.SourceGenerators.Tests](../Rin.SourceGenerators.Tests/ReadMe.md). The easiest way to develop language-related features is to start with unit tests.

## Build and test

```
dotnet build Engine/Rin.SourceGenerators/Rin.SourceGenerators.csproj
dotnet test Engine/Rin.SourceGenerators.Tests/Rin.SourceGenerators.Tests.csproj -p:RinShadeSkipCompile=true
```

## How To?
### How to debug?
- Use the [launchSettings.json](Properties/launchSettings.json) profile.
- Debug tests.

### How can I determine which syntax nodes I should expect?
Consider using the Roslyn Visualizer tool window, which allows you to observe the syntax tree.

### How to learn more about wiring source generators?
Watch the walkthrough video: [Let’s Build an Incremental Source Generator With Roslyn, by Stefan Pölz](https://youtu.be/azJm_Y2nbAI)
The complete set of information is available in [Source Generators Cookbook](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md).
