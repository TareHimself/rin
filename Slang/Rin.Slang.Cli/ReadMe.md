# Rin.Slang.Cli

Command line tool, built as the executable `rin-slang`. References `Rin.Slang.Compiler` and `Rin.Slang.Discovery`.

## Start here

- `Program.cs`: argument parsing and the three commands.

## Commands

Taken from the usage text in `Program.cs`:

```
rin-slang compile <input.slang> [-o <output.crsh>] [-I <searchPath>]... [-D <NAME>=<VALUE>]... [-A <ALIAS>=<path>]...
rin-slang discover <searchRoot> [--prefix <prefix>]...
rin-slang compile-referenced --prefix <prefix> --repo-root <path> --output <dir> [--scan <path>]... [-D <NAME>=<VALUE>]...
```

## Build and run

```
dotnet build Slang/Rin.Slang.Cli/Rin.Slang.Cli.csproj
```

## Gotchas

- `compile` and `compile-referenced` go through `Rin.Slang.Compiler`, so they need the real `Rin.Slang.Native` library, not a stub.
