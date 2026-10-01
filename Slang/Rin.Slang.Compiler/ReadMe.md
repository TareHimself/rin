# Rin.Slang.Compiler

Compiles `.slang` source using the native Slang wrapper. Packed as `TareHimself.Rin.Slang.Compiler` (AOT compatible, unsafe code allowed).

## Where it fits

- Depends on `Rin.Slang` (project reference, or package when `DepMode` is `online`) and on the `TareHimself.Rin.Slang.Native` package.
- Used by `Rin.Slang.Cli`, `Shade/Rin.Shade.MSBuild` and `Shade/Rin.Shade.CpuTests`.
- Tested by `Rin.Slang.Compiler.Tests`.

## Start here

- `ShaderCompiler.cs` and `ShaderCompilerOptions.cs`: the entry point. Options hold search paths, defines, path aliases and a portable root.
- `Native.cs`: the P/Invoke declarations for `Rin.Slang.Native`.
- `SlangSessionBuilder.cs`, `SlangSession.cs`, `SlangModule.cs`, `SlangEntryPoint.cs`, `SlangComponent.cs`, `SlangBlob.cs`: thin wrappers over the native objects.
- `HostComputeCompiler.cs` and `SlangSharedLibrary.cs`: compiled host (CPU) entry points and function lookup in them.
- `SlangCompileException.cs`, `NotAShaderException.cs`: errors.

## Build and test

```
dotnet build Slang/Rin.Slang.Compiler/Rin.Slang.Compiler.csproj
dotnet test Slang/Rin.Slang.Compiler.Tests/Rin.Slang.Compiler.Tests.csproj
```

## Gotchas

- Restore needs a `TareHimself.Rin.Slang.Native` package in `.feed/` (real or stub). See [native/ReadMe.md](../../native/ReadMe.md).
- A stub package restores fine but contains no Slang, so real compilation needs the real package (or the fake, for tests).
