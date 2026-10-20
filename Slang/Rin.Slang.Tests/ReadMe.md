# Rin.Slang.Tests

NUnit tests for `Rin.Slang`, the package format: writing and reading a package, and source hashing. Targets `net10.0`, not packable.

```
dotnet test Slang/Rin.Slang.Tests/Rin.Slang.Tests.csproj
```

References: `Rin.Slang` only. Needs no native Slang and no fake. CI runs it in the `test` job of `.github/workflows/ci.yml` on stub packages, with `-p:RinShadeSkipCompile=true`.
