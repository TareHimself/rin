# Rin conventions

Instructions for coding agents and contributors working in this repository.

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

## Comments

Follow standard C# comment conventions.

- Use `//` for inline comments.
- Use XML documentation for types and members (classes, structs, interfaces, enums, methods, properties),
  with the tags on their own lines and the content between them:

  ```csharp
  /// <summary>
  /// Transpiles every shader class reachable from the project's compilation.
  /// </summary>
  public sealed class ShaderCompiler
  ```

- Comment the non-obvious "why", not what the code already says.

## Performance

Prefer zero-allocation or low-allocation code, and use pooling where it fits.

- Avoid allocating in per-frame, per-draw and per-item paths: no LINQ, closures, boxing or short-lived
  collections in hot code.
- Prefer `Span<T>`, `ReadOnlySpan<T>`, `stackalloc`, structs and `in`/`ref` parameters where they avoid copies
  or heap allocations.
- Rent from `ArrayPool<T>` or an existing pool for temporary buffers, and return them, instead of
  allocating a new array each time.
- Reuse and clear a collection instead of creating a new one each call.
- Build-time tooling (the Rin.Shade transpiler, source generators, MSBuild tasks) may favour clarity
  over allocation.
