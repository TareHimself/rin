# native

C++ native modules, the Conan and CMake build for them, and the C# fakes used in place of them in CI. Each real module is built into a shared library and packed as a NuGet package named `TareHimself.<project name>` into the local feed `.feed/` (configured in `NuGet.Config`). The managed projects reference those packages (versions `1.*` in `Directory.Packages.props`).

| Project | What it is |
| --- | --- |
| [Rin.Native](Rin.Native/ReadMe.md) | Core native helpers: memory, platform, SDF (msdfgen) and video (webmdx). Used by `Rin.Core`. |
| [Rin.Audio.Miniaudio.Native](Rin.Audio.Miniaudio.Native/ReadMe.md) | Miniaudio wrapper. Used by `Rin.Audio.Miniaudio`. |
| [Rin.Graphics.Vulkan.Native](Rin.Graphics.Vulkan.Native/ReadMe.md) | Vulkan, windowing and memory allocator wrapper. Used by `Rin.Graphics.Vulkan`. |
| [Rin.Slang.Native](Rin.Slang.Native/ReadMe.md) | Small C++ wrapper over a prebuilt Slang release. Used by `Rin.Slang.Compiler`. |
| [Fakes](Fakes/ReadMe.md) | NativeAOT C# stand-ins for `Rin.Native` and `Rin.Slang.Native`, used by tests. |

Shared build files in this folder:

- `base_recipe.py`: the Conan `BaseRecipe` every module's `conanfile.py` extends. It lays out `build-release` or `build-debug`, writes the CMake toolchain and renames the generated preset to `Rin-Release` or `Rin-Debug`.
- `make_nuspec_cli.py` and `dotnet.py`: generate a nuspec from a directory of built binaries (see below).

## Building and packing

Run from the repo root through [uv](https://docs.astral.sh/uv/), which provides Python, Conan, CMake and Task from `pyproject.toml` (you also need a C++ toolchain and, for the Vulkan module, the Vulkan SDK):

```
uv run task pack-all
```

This runs `pack-native`, `pack-audio-miniaudio-native`, `pack-graphics-vulkan-native` and `pack-slang-native` from the root `Taskfile.yml`. Each one runs the module's own `task build` (in its `Taskfile.yml`: `conan install . --build=missing -s build_type=Release`, then `cmake --preset Rin-Release --fresh` and `cmake --build --preset Rin-Release`), then:

```
dotnet pack project.csproj /p:NuspecFile='specs/Release.nuspec' /p:NuspecBasePath='specs' --output <repo>/.feed
```

Each module's `Taskfile.yml` also has `buildd` for a Debug build.

How the nuspec is produced: a CMake post-build step in each module runs `make_nuspec_cli.py`, which writes `specs/<CMAKE_BUILD_TYPE>.nuspec`. It lists every `*.dll`, `*.so`, `*.so.*` and `*.dylib` in the build output directory and maps each to `runtimes/<rid>/native/<file>`, where the RID comes from `dotnet --info`. The package id is `<author>.<name>` (`TareHimself.<project name>`). `project.csproj` in each module is only a shell for `dotnet pack`; the packages contain no managed assembly.

## The three ways a native library shows up

1. **Real package.** Built locally by `uv run task pack-all`, or in CI by the `build-native-slang` job (only `Rin.Slang.Native`, packed as version `1.0.1`). Contains the real binaries. Needed to actually compile shaders, run the Vulkan backend or play audio.
2. **Stub package.** `scripts/pack_native_stubs.py <feed_dir>` reads every `*.Native` package id from `Directory.Packages.props` and packs a nuspec containing one placeholder file, `runtimes/win-x64/native/stub.dll`. It satisfies restore and build so nothing needs Conan, CMake or the Vulkan SDK. Calling into the library would fail. The stubs use version `1.0.0` (the `*` parts of the floating `1.*` become `0`).
3. **Fake.** The C# projects in `native/Fakes`, published with NativeAOT by `scripts/publish_native_fakes.py <rid>` into real native libraries that export the same entry points. Tests that do call native code (`Rin.GLTF.Tests` for `Rin.Native`, `Rin.Slang.Compiler.Tests` for `Rin.Slang.Native`) install a DllImport resolver that loads the fake instead of the package. Fakes are not packages and are not put in the feed.

In CI (`.github/workflows/ci.yml`):

- Every `test` matrix job packs stubs into `.feed`. Jobs with `fakes: true` also publish the fakes.
- `build-native-slang` also packs stubs, builds the real `Rin.Slang.Native` with Conan and CMake, packs it as `1.0.1` into the same feed, and uploads the feed as the `native-feed` artifact. The comment in the workflow says the higher version is so the floating `1.*` reference resolves to it instead of the stub.
- `test-real-slang` downloads that artifact into `.feed` and runs the tests and shader compilation that need real Slang.

## Gotchas

- Restoring on a clean checkout fails until `.feed/` contains the native packages. Run `uv run task pack-all` (real) or `uv run python scripts/pack_native_stubs.py .feed` (stubs).
- After changing a module's C++ source, re-run its `pack-*` task.
- Rebuilding a package at the same version does not refresh the copy in the global NuGet cache, so restore keeps using the old one. Run `uv run task force-restore`, which clears the whole global cache (slow, every package is downloaded again) and then runs `dotnet restore` on `rin.sln`.
