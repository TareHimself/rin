# native

C++ native modules, the [silo](https://github.com/TareHimself/silo) and CMake build for them, and the C# fakes used in place of them in CI. Each real module is built into a shared library and packed as a NuGet package named `TareHimself.<project name>` into the local feed `.feed/` (configured in `NuGet.Config`). The managed projects reference those packages (versions `1.*` in `Directory.Packages.props`).

| Project | What it is |
| --- | --- |
| [Rin.Native](Rin.Native/ReadMe.md) | Core native helpers: memory, platform, SDF (msdfgen) and video (webmdx). Used by `Rin.Core`. |
| [Rin.Audio.Miniaudio.Native](Rin.Audio.Miniaudio.Native/ReadMe.md) | Miniaudio wrapper. Used by `Rin.Audio.Miniaudio`. |
| [Rin.Graphics.Vulkan.Native](Rin.Graphics.Vulkan.Native/ReadMe.md) | Vulkan, windowing and memory allocator wrapper. Used by `Rin.Graphics.Vulkan`. |
| [Rin.Slang.Native](Rin.Slang.Native/ReadMe.md) | Small C++ wrapper over a prebuilt Slang release. Used by `Rin.Slang.Compiler`. |
| [Fakes](Fakes/ReadMe.md) | NativeAOT C# stand-ins for `Rin.Native` and `Rin.Slang.Native`, used by tests. |

Shared build files in this folder:

- `silo.config.toml`: where silo finds the packages the modules depend on by name. It lists this folder's own `recipes/`, the `silo-recipes` registry at one commit, and the commits `webmdx` and `rwin` are built from (each keeps its recipe in its own repository).
- `recipes/`: recipes for packages no registry has. `slang` takes upstream's prebuilt release, and `vulkan-memory-allocator` is one header.

Each module has a `silopkg.py` (its silo recipe: dependencies and how to build), a `CMakeLists.txt`, a `CMakePresets.json` that includes the presets silo writes, and a `project.csproj` that is only a shell for `dotnet pack`.

## Building and packing

Run from the repo root through [uv](https://docs.astral.sh/uv/), which provides Python and silo from `pyproject.toml`. silo is pinned to one commit there. You also need a C++ toolchain and git. silo fetches CMake and Ninja itself and builds every C++ dependency from source into its store (`~/.silo`, or `SILO_HOME`), so nothing else is installed, the Vulkan SDK included.

```
uv run silo profile detect
uv run python scripts/pack_native.py
```

`silo profile detect` writes this machine's compiler into silo's default profile and is needed once. `pack_native.py` packs every module, or the ones named (`uv run python scripts/pack_native.py Rin.Native Rin.Slang.Native`). For each module it:

1. Runs `silo build native/<module> --report native/<module>/build-report`. silo builds the dependencies it does not have in its store yet, then the module. The module's install step puts the library and every shared library it loads (`SILO_RUNTIME_LIBRARIES`) into one `bin` folder of its store entry.
2. Writes `native/<module>/specs/Release.nuspec`, which maps every `*.dll`, `*.so`, `*.so.*` and `*.dylib` in that `bin` folder to `runtimes/<rid>/native/<file>`. The RID comes from `dotnet --info`, the package id is `TareHimself.<module>`, and the version is the recipe's unless `--version` is given. The packages contain no managed assembly.
3. Runs `dotnet pack project.csproj -p:NuspecFile=<nuspec> -p:NuspecBasePath=<bin folder> --output .feed` (`--feed` picks another folder).
4. Deletes that package version from the global NuGet cache, so the next restore unpacks the new build.

When a build fails, the error names the failed command, and the output of each package's commands is in `native/<module>/build-report/logs`.

For a Debug build or an IDE, `uv run silo sync native/<module>` builds the dependencies and writes `.silo/` with the `Rin-Debug` and `Rin-Release` presets, which build into `build-debug` and `build-release`. `uv run silo run -C native/<module> -- <command>` runs a command such as `cmake --preset Rin-Debug` with silo's compiler and tools set up.

## The three ways a native library shows up

1. **Real package.** Built locally by `uv run python scripts/pack_native.py`, or in CI by the `build-native-slang` job (only `Rin.Slang.Native`, packed as version `1.0.1`). Contains the real binaries. Needed to actually compile shaders, run the Vulkan backend or play audio.
2. **Stub package.** `scripts/pack_native_stubs.py <feed_dir>` reads every `*.Native` package id from `Directory.Packages.props` and packs a nuspec containing one placeholder file, `runtimes/win-x64/native/stub.dll`. It satisfies restore and build so nothing needs silo or a C++ toolchain. Calling into the library would fail. The stubs use version `1.0.0` (the `*` parts of the floating `1.*` become `0`).
3. **Fake.** The C# projects in `native/Fakes`, published with NativeAOT by `scripts/publish_native_fakes.py <rid>` into real native libraries that export the same entry points. Tests that do call native code (`Rin.GLTF.Tests` for `Rin.Native`, `Rin.Slang.Compiler.Tests` for `Rin.Slang.Native`) install a DllImport resolver that loads the fake instead of the package. Fakes are not packages and are not put in the feed.

In CI (`.github/workflows/ci.yml`):

- Every `test` matrix job packs stubs into `.feed`. Jobs with `fakes: true` also publish the fakes.
- `build-native-slang` also packs stubs, builds and packs the real `Rin.Slang.Native` as `1.0.1` into the same feed with `pack_native.py` (building Slang from scratch each run, since without a lock file a cached store could hide a changed dependency), and uploads the feed as the `native-feed` artifact. The comment in the workflow says the higher version is so the floating `1.*` reference resolves to it instead of the stub.
- `test-real-slang` downloads that artifact into `.feed` and runs the tests and shader compilation that need real Slang.

## Gotchas

- Restoring on a clean checkout fails until `.feed/` contains the native packages. Run `uv run python scripts/pack_native.py` (real) or `uv run python scripts/pack_native_stubs.py .feed` (stubs).
- After changing a module's C++ source, pack it again (`uv run python scripts/pack_native.py <module>`) and restore.
- silo builds a module from a snapshot of its folder as it is on disk, so uncommitted changes are built. The folder has to be inside the git repository.
- A recipe file (`silopkg.py`) with Windows line endings is refused by silo. `.gitattributes` keeps the repository on LF.
- macOS is not supported: silo does not build there yet.
