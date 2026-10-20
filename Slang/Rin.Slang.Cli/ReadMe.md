# Rin.Slang.Cli

Command line tool, built as the executable `rin-slang`, for compiling shaders and finding the ones C# code references.

```
dotnet build Slang/Rin.Slang.Cli/Rin.Slang.Cli.csproj
```

| Command | Usage |
| --- | --- |
| `compile` | `rin-slang compile <input.slang> [-o <output.crsh>] [-I <searchPath>]... [-D <NAME>=<VALUE>]... [-A <ALIAS>=<path>]...` |
| `discover` | `rin-slang discover <searchRoot> [--prefix <prefix>]...` |
| `compile-referenced` | `rin-slang compile-referenced --prefix <prefix> --repo-root <path> --output <dir> [--scan <path>]... [-D <NAME>=<VALUE>]...` |

References: `Rin.Slang.Compiler`, `Rin.Slang.Discovery`.

- `compile` and `compile-referenced` go through `Rin.Slang.Compiler`, so they need the real `Rin.Slang.Native` package, not a stub.
