# Rin.Slang.Compiler

Compiles `.slang` source using the native Slang wrapper. Packed as `TareHimself.Rin.Slang.Compiler` (AOT compatible, unsafe code allowed).

## Where it fits

- Depends on `Rin.Slang` (project reference, or package when `DepMode` is `online`) and on the `TareHimself.Rin.Slang.Native` package.
- Used by `Rin.Slang.Cli`, `Shade/Rin.Shade.MSBuild` and `Shade/Rin.Shade.CpuTests`.
- Tested by `Rin.Slang.Compiler.Tests`.

## Start here

- `ShaderCompiler.cs` and `ShaderCompilerOptions.cs`: the entry point. Options hold search paths, defines, path aliases and a portable root. `IsUpToDate` compares a `.crsh` package's source hash with the current source and its includes; the static overload takes the options instead of a compiler, so a build can decide to skip before paying for a Slang session.
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
- A `ShaderCompiler` owns one Slang session with its own global session. Use each instance from one thread at a time; separate instances can compile on separate threads. Creating one costs about 450 ms, compiling a small shader under 10 ms.
