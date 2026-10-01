# Rin.Core.Tests

NUnit tests for [Rin.Core](../Rin.Core/ReadMe.md).

## Where it fits

- References: Rin.Core only.

## Start here

Folders mirror Rin.Core:

- `Shared/`: bounds, math, curves, dispatcher, id factory and providers tests.
- `Views/`: view tests (switcher, scroll events, composite and graphics views) with test doubles in `Views/TestDoubles.cs`.
- `Graphics/ResourceHandleTests.cs`.
- `UnitTest1.cs`: a file at the project root outside the folder layout.

## Run

```
dotnet test Engine/Rin.Core.Tests/Rin.Core.Tests.csproj -p:RinShadeSkipCompile=true
```

CI passes `-p:RinShadeSkipCompile=true` for this project. Test layout rules are in `AGENTS.md`.
