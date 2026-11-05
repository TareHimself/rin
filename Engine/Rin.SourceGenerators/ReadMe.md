# Rin.SourceGenerators

Roslyn incremental source generators, consumed by Rin.Core, Rin.World, Rin.World.Tests, Rin.World.AotSmoke, Examples and the experiments as an analyzer (`OutputItemType="Analyzer"`, `ReferenceOutputAssembly="false"`). Not packable.

| Generator | Triggers on |
| --- | --- |
| `AudioEffectGenerator` | Types marked `Rin.Core.Audio.Effects.AudioEffectAttribute` |
| `ComponentRegistrationGenerator` | Structs implementing `Rin.World.IComponentData` or `Rin.World.ITagData`, directly or through another interface. Emits one `[ModuleInitializer]` per assembly that registers them with `Rin.World.ComponentRegistry`, and nothing for an assembly without any. Generic, private, protected, file-local and ref structs get warning `RIN00024` instead. |
| `GraphicsShaderGenerator` | Properties marked with an attribute deriving from `Rin.Core.Graphics.Shaders.ShaderAttribute` |
| `ProviderResolvedGenerator` | Properties marked `Rin.Core.Shared.Providers.ResolvedAttribute` |

```
dotnet build Engine/Rin.SourceGenerators/Rin.SourceGenerators.csproj
dotnet test Engine/Rin.SourceGenerators.Tests/Rin.SourceGenerators.Tests.csproj -p:RinShadeSkipCompile=true
```

- Build this project to see the generated code in the IDE. To debug a generator, use the `DebugRoslynSourceGenerator` profile in `Properties/launchSettings.json` (it targets Rin.Core) or debug the tests in [Rin.SourceGenerators.Tests](../Rin.SourceGenerators.Tests/ReadMe.md).

References: Microsoft.CodeAnalysis.CSharp, Microsoft.CodeAnalysis.CSharp.Workspaces, Microsoft.CodeAnalysis.Analyzers. Background reading: the [Source Generators Cookbook](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md).
