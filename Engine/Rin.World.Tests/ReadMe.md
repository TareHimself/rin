# Rin.World.Tests

NUnit tests for [Rin.World](../Rin.World/ReadMe.md).

## Where it fits

- References: Rin.World only (Rin.Core comes through it).

## Start here

- `TestDoubles.cs`: shared fakes used across subsystems (per `AGENTS.md`, these stay at the project root).
- `WorldTimeScaleTests.cs`: tests for `World` at the root namespace.
- `Components/`, `Graphics/`, `Physics/`, `Skinning/`: tests mirroring the subsystem folders in Rin.World.

## Run

```
dotnet test Engine/Rin.World.Tests/Rin.World.Tests.csproj -p:RinShadeSkipCompile=true
```

CI passes `-p:RinShadeSkipCompile=true` for this project. Test layout rules are in `AGENTS.md`.
