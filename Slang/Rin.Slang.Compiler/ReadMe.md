# Rin.Slang.Compiler

Compiles `.slang` source into `Rin.Slang` packages through the native Slang wrapper, and checks whether a package is still up to date with its source. Packed as `TareHimself.Rin.Slang.Compiler` (AOT compatible, unsafe code allowed).

```
dotnet build Slang/Rin.Slang.Compiler/Rin.Slang.Compiler.csproj
dotnet test Slang/Rin.Slang.Compiler.Tests/Rin.Slang.Compiler.Tests.csproj
```

References: `Rin.Slang` (project reference when `DepMode` is `local_file`, package when `online`) and the `TareHimself.Rin.Slang.Native` package. Used by `Rin.Slang.Cli`, `Shade/Rin.Shade.MSBuild` and `Shade/Rin.Shade.CpuTests`. Tested by [Rin.Slang.Compiler.Tests](../Rin.Slang.Compiler.Tests/ReadMe.md).

- Restore needs a `TareHimself.Rin.Slang.Native` package in `.feed/`. A stub restores fine but contains no Slang, so real compilation needs the real package (or the fake, for tests). See [native/ReadMe.md](../../native/ReadMe.md).
