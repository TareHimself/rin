# Rin.World.Tests

NUnit tests for [Rin.World](../Rin.World/ReadMe.md).

## Where it fits

- References: Rin.World only (Rin.Core comes through it).

## Start here

- `TestDoubles.cs`: shared fakes used across subsystems (per `AGENTS.md`, these stay at the project root).
- `WorldTimeScaleTests.cs`: tests for `World` at the root namespace.
- `Components/`, `Graphics/`, `Physics/`, `Skinning/`: tests mirroring the subsystem folders in Rin.World. `Physics/PhysicsQueryTests.cs` and `Physics/ContactEventTests.cs` run the real Bepu system (queries, triggers, begin and end events, sleeping and destroyed bodies); `Components/PhysicsContactDispatchTests.cs` checks the world routes events to components using the fake's `EmitContact`.

## Run

```
dotnet test Engine/Rin.World.Tests/Rin.World.Tests.csproj -p:RinShadeSkipCompile=true
```

CI passes `-p:RinShadeSkipCompile=true` for this project. Test layout rules are in `AGENTS.md`.
