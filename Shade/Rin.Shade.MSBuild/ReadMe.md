# Rin.Shade.MSBuild

The MSBuild task `Rin.Shade.MSBuild.CompileRinShadeShaders`. In one in-process step it transpiles every `[Shader("...")]` class (via `ShadeEmitter`), compiles the Slang with `Rin.Slang.Compiler`'s `ShaderCompiler`, and returns the resulting `.crsh` files as `EmbeddedResource` items.

## Where it fits

- References `Rin.Shade.Transpiler`, `Slang/Rin.Slang.Compiler` and `Microsoft.Build.Utilities.Core`.
- Not referenced by any project. `msbuild/RinShade.targets` loads its DLL with `UsingTask` from `Shade/Rin.Shade.MSBuild/bin/Release/net10.0/Rin.Shade.MSBuild.dll`.
- The csproj sets a RID and `CopyLocalLockFileAssemblies` so Roslyn and the native Slang DLLs land in its own output, and keeps the output path free of the RID folder so the targets file can use a fixed path.

## Using it from a project

Set these before importing `msbuild/RinShade.targets` (see `Engine/Rin.Core/Rin.Core.csproj`):

- `RinShadeDiscoverPrefix`: only shaders whose `[Shader]` path starts with this prefix are compiled.
- `RinShadeOutputSubpath`: output folder relative to `$(IntermediateOutputPath)`, also the prefix of the embedded resource's logical name.

Optional properties, from the targets file:

- `RinShadeSkipCompile=true`: skips the whole step. CI passes it for jobs that never need real shaders.
- `RinShadeDumpGenerated=true`: also writes the emitted Slang to `GeneratedShaders/` under the output directory (override with `RinShadeGeneratedDirectory`).

## Gotchas

- The task project is built in Release only when its DLL does not exist yet (target `BuildRinShadeMSBuildTask`). It is not rebuilt on later builds, because MSBuild node reuse keeps the DLL loaded and a rebuild would fail with a file-in-use error. After changing the transpiler, rebuild it by hand, for example `dotnet build Shade/Rin.Shade.MSBuild/Rin.Shade.MSBuild.csproj -c Release` (stop build servers first if the DLLs are locked).
- The on-demand build does not restore. On a clean checkout restore the solution first, as the CI workflow does (`dotnet restore rin.sln`), or the build fails with NETSDK1004.
- Compiling needs the real native Slang library.
