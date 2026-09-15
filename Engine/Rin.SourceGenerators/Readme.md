# Rin Source Generators

Roslyn source generators consumed by Rin.Core (referenced as an analyzer via `OutputItemType="Analyzer"`).

## Content
### Rin.SourceGenerators
Implementations of the source generators.
**You must build this project to see the result (generated code) in the IDE.**

- [AudioEffectGenerator.cs](AudioEffectGenerator.cs)
- [GraphicsShaderGenerator.cs](GraphicsShaderGenerator.cs)

### Rin.SourceGenerators.Tests
Unit tests for the source generators. The easiest way to develop language-related features is to start with unit tests.

## How To?
### How to debug?
- Use the [launchSettings.json](Properties/launchSettings.json) profile.
- Debug tests.

### How can I determine which syntax nodes I should expect?
Consider using the Roslyn Visualizer tool window, which allows you to observe the syntax tree.

### How to learn more about wiring source generators?
Watch the walkthrough video: [Let’s Build an Incremental Source Generator With Roslyn, by Stefan Pölz](https://youtu.be/azJm_Y2nbAI)
The complete set of information is available in [Source Generators Cookbook](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md).