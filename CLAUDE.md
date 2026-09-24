# Rin conventions

## Test organization

Test projects (`Rin.Core.Tests`, `Rin.World.Tests`, ...) mirror the folder structure of the
project under test, one folder per subsystem (e.g. `Rin.World\Graphics\Default\DefaultRenderSystem.cs`
→ `Rin.World.Tests\Graphics\DefaultRenderSystemTests.cs`), with the test file's namespace matching
its folder (`Rin.World.Tests.Graphics`, not the flat `Rin.World.Tests`). A class at the project's
root namespace (e.g. `World.cs`) gets its test at the test project's root, with no folder.

Shared test infrastructure used across multiple subsystems (fakes/test doubles like
`FakeRenderSystem`, `FakePhysicsSystem`, `TestMeshComponent`) stays in `TestDoubles.cs` at the test
project's root rather than living under any one subsystem folder.

When a single source type spans two subsystems' worth of behavior (e.g. a test file covering both
a `Graphics` handle type and a `Physics` handle type), split it into one test file per subsystem
folder rather than picking one folder for the combined file.
