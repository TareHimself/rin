# Rin.Core.Tests

NUnit tests for [Rin.Core](../Rin.Core/ReadMe.md).

## Where it fits

- References: Rin.Core only.

## Start here

Folders mirror Rin.Core:

- `Audio/Effects/ParameterSlotsTests.cs`: the effect parameter triple buffer, including a concurrent writer and reader checking for torn reads.
- `Shared/`: bounds, math, curves, dispatcher, id factory and providers tests.
- `Views/`: view tests (switcher, scroll events, colour and border radius value types, composite and graphics views) with test doubles in `Views/TestDoubles.cs`, whose `FakeSurface` has an `InMemoryClipboard` for the copy, cut and paste tests.
- `Graphics/ResourceHandleTests.cs`.
- `UnitTest1.cs`: a file at the project root outside the folder layout.

## Run

```
dotnet test Engine/Rin.Core.Tests/Rin.Core.Tests.csproj -p:RinShadeSkipCompile=true
```

CI passes `-p:RinShadeSkipCompile=true` for this project. Test layout rules are in `AGENTS.md`.
