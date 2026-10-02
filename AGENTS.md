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

## Maintainability

Code must be maintainable by humans and AI alike. Generated or quickly written code is not an exception:
if a person cannot read it, follow it and change it safely, it is not done.

- Write code a reviewer can understand without the author: clear names, small focused methods, one level of
  abstraction at a time, and structure that matches the surrounding code.
- Do not leave dense, clever or sprawling code in place of a straightforward version. Simplify it before
  committing it.
- Keep changes easy to modify: avoid hidden coupling and duplicated logic, and cover behaviour with tests so
  the next change, by a person or an agent, can be made with confidence.
- Prefer explicit, discoverable designs (named types and members) over conventions that only work if you
  already know the trick.

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
- Keep the content short: one line of text beats a multi-sentence paragraph. If a member's behavior is
  already obvious from its name or a one-line body, skip the doc or cut its content to a single line instead
  of restating the implementation in prose. Save the longer explanation for the type-level summary/remarks
  where a non-obvious mechanism genuinely needs it.
- Keeping the content to one line never means collapsing the tags onto that same line. `<summary>` (and every
  other XML doc tag) always keeps its three-line form - opening tag, content, closing tag, each on their own
  line - exactly like the example above, even when the content is a single short sentence:

  ```csharp
  /// <summary>
  ///     The value as of the last <see cref="TryConsume" />, or the initial value.
  /// </summary>
  public T Current => ...;
  ```

  not:

  ```csharp
  /// <summary>The value as of the last <see cref="TryConsume" />, or the initial value.</summary>
  public T Current => ...;
  ```

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

## Tooling

- Python tooling runs through uv (`uv run ...`), with versions pinned in `pyproject.toml` and `uv.lock`. Commit `uv.lock` when the dependencies change.

## Documentation

Keep the docs true. A change is not done until the docs it affects are updated in the same change.

- Update the `ReadMe.md` of every project whose behavior, public surface, build or test commands, references or
  key files changed. Fix claims that became wrong, not only the ones you added.
- Update `ARCHITECTURE.md` when a change alters the layers, the per-frame flow, or how the major subsystems
  connect, and the diagrams and `Shade/ReadMe.md` pipeline description when the shader pipeline changes.
- When you add or remove a project, add or remove its `ReadMe.md` and its entry in the "All READMEs" list in the
  root `ReadMe.md`. Folders do not get a `ReadMe.md` of their own, except `Shade/` and `native/`, which document a
  subsystem.
- Only state what you verified in the code. Do not describe behavior from file names.
- Examples in docs must be real: run them (for example `rin-shade compile` for a shader) and paste the actual output.
- Follow the same style as the existing docs: plain language, no em dashes, commands that work from the repo root.
- Design plans and handoff notes are not repo docs. Do not commit them.

