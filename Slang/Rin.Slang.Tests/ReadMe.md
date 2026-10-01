# Rin.Slang.Tests

NUnit tests for `Rin.Slang` (the package format). Targets `net10.0`, not packable, references only `Rin.Slang`.

## Start here

- `ShaderPackageTests.cs`: package write and read.
- `ShaderSourceHashTests.cs`: source hashing.

## Run

```
dotnet test Slang/Rin.Slang.Tests/Rin.Slang.Tests.csproj
```

## Notes

- Needs no native Slang and no fake. CI runs it in the `test` job of `.github/workflows/ci.yml` on stub packages, with `-p:RinShadeSkipCompile=true`.
