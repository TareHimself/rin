# Native conventions

Rules for the C++ modules, their packaging and the fakes. They add to the root `AGENTS.md`. How packaging works is in `ReadMe.md`.

## Fakes

- Tests that call native code use the fakes in `native/Fakes`, not the real libraries. When you add or change an exported function that a test project calls, update the matching fake, or CI fails in a way that looks unrelated to your change.
- Fakes are not packages and are not put in the feed. Keep them out of `.feed`.

## Packages and versions

- A native package is never committed. `specs/` and `build-*` are ignored, and no `.dll`, `.so` or `.nupkg` belongs in git. Build and pack with `uv run python scripts/pack_native.py`.
- Add a new `*.Native` package id to `Directory.Packages.props`. The stub packaging reads the ids from there, so nothing else lists them.
- The stub packages are version `1.0.0`. CI builds the real `Rin.Slang.Native` as `1.0.1` so the floating `1.*` reference picks it. Do not give a real and a stub package the same version: the shared NuGet package cache would reuse the stub.
- Nuspec `src` entries are file names, and `pack_native.py` passes the folder holding them as `NuspecBasePath`. Do not write absolute paths into a nuspec.

## Building

- silo comes from `pyproject.toml` and `uv.lock`, pinned to one commit, and is run with `uv run`. Do not rely on a system Python or a global CMake. To move to a newer silo, change `rev` under `[tool.uv.sources]`, run `uv lock` and commit `uv.lock`.
- C++ dependencies are declared in the module's `silopkg.py` with `self.dep("name@version")` and found through `silo.config.toml`. Do not add `FetchContent`, a download or a system `find_package` to a `CMakeLists.txt`: silo's build has no git on its `PATH` and does not search the machine. A package no registry has gets a recipe in `native/recipes/<name>/<version>/silopkg.py`.
- Every git entry in `silo.config.toml` names a full commit hash. Move one by editing the hash.
- Each module's `silopkg.py` is complete by itself. silo hashes that one file to decide whether to rebuild, so a base class in another file would change builds without changing their id.
- Link C++ dependencies statically (silo's default) unless they only exist as a shared library, as Slang does, so each package stays one DLL where it can.
- A module installs its library and `SILO_RUNTIME_LIBRARIES` into `bin`, and its recipe packages `bin`. `pack_native.py` packs what is there.
- Set `CMAKE_CXX_STANDARD` after `project()`. The silo toolchain file is read at `project()` and sets the profile's standard, which defaults to 17.
- The `slang` recipe downloads a prebuilt Slang release. Do not add Slang's own source to the build.

## Docs

- Update `native/ReadMe.md` and the module's `ReadMe.md` when a module's exports, dependencies or build steps change.
