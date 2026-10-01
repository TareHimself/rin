# Native conventions

Rules for the C++ modules, their packaging and the fakes. They add to the root `AGENTS.md`. How packaging works is in `ReadMe.md`.

## Fakes

- Tests that call native code use the fakes in `native/Fakes`, not the real libraries. When you add or change an exported function that a test project calls, update the matching fake, or CI fails in a way that looks unrelated to your change.
- Fakes are not packages and are not put in the feed. Keep them out of `.feed`.

## Packages and versions

- A native package is never committed. `specs/` and `build-*` are ignored, and no `.dll`, `.so` or `.nupkg` belongs in git. Build and pack through the `uv run task pack-*` targets.
- Add a new `*.Native` package id to `Directory.Packages.props`. The stub packaging reads the ids from there, so nothing else lists them.
- The stub packages are version `1.0.0`. CI builds the real `Rin.Slang.Native` as `1.0.1` so the floating `1.*` reference picks it. Do not give a real and a stub package the same version: the shared NuGet package cache would reuse the stub.
- Nuspec `src` paths are generated relative to the nuspec, and the pack tasks pass `NuspecBasePath=specs`. Do not write absolute paths into a nuspec.

## Building

- Python tooling (Conan, CMake, Task) comes from `pyproject.toml` and `uv.lock`, run with `uv run`. Do not rely on a system Python or a global Conan, CMake or Task. Add or update a tool with `uv add --group native <package>` and commit `uv.lock`.
- Release builds go through Conan and CMake (`uv run task build`), Debug through `uv run task buildd`. CI passes `-s compiler.cppstd=20` to Conan because a fresh runner's profile defaults to an older standard. The CMake files already set C++20.
- Rin.Slang.Native downloads a prebuilt Slang release. Do not add Slang's own source to the build.

## Docs

- Update `native/ReadMe.md` and the module's `ReadMe.md` when a module's exports, dependencies or build steps change.
