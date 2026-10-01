# Rin.Slang

The compiled shader package format, with no native dependency. Packed as `TareHimself.Rin.Slang` (AOT compatible).

## Where it fits

- `Rin.Slang.Compiler` writes these types (project reference when `DepMode` is `local_file`, package reference when `online`).
- `Engine/Rin.Graphics.Vulkan` references it to read compiled shaders.
- `Rin.Slang.Tests` tests it.

## Start here

- `CompiledShader.cs`, `CompiledStage.cs`, `ShaderKind.cs`: the compiled result.
- `ShaderManifest.cs` and `SlangReflectionData.cs`: manifest and reflection data (with the `*JsonContext.cs` source-generated JSON contexts).
- `ShaderPackageWriter.cs` and `ShaderPackageReader.cs`: serialize and load a package.
- `ShaderSourceHash.cs`: hashing of shader source.

## Build and test

```
dotnet build Slang/Rin.Slang/Rin.Slang.csproj
dotnet test Slang/Rin.Slang.Tests/Rin.Slang.Tests.csproj
```
