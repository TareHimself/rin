# Rin.Shade.MSBuild

The MSBuild task `Rin.Shade.MSBuild.CompileRinShadeShaders`. In one in-process step it transpiles every `[Shader("...")]` class (via `ShadeEmitter`), compiles the Slang with `Rin.Slang.Compiler`'s `ShaderCompiler`, and returns the resulting `.crsh` files as `EmbeddedResource` items.

A Slang compile error is logged against the C# file and line it was lowered from: the task parses Slang's `path(line): error code: message` output with `SlangDiagnostic` and looks the line up in the shader's `SlangSourceMap`. Lines without a C# origin (type declarations, bindings) are reported against the scratch `.slang` file instead. The error code is `SLANG` followed by Slang's own number.

A shader whose `.crsh` under `obj` still carries the hash of its current Slang (`ShaderCompiler.IsUpToDate`) is reused without compiling, and when every shader is up to date no Slang session is created at all, so a build with no shader changes spends about a millisecond here. `dotnet clean` removes the packages, like any other `obj` output.

The stale shaders are compiled on worker threads, each with its own `ShaderCompiler`, since a Slang session must be used from one thread at a time. The default thread count is one per 64 shaders, capped at the processor count: creating a session costs about 450 ms and compiling a shader under 10 ms, so with Rin's shader counts one thread is fastest.

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
- `RinShadeMaxParallelism=N`: compile up to N shaders at once. `1` is serial; unset or `0` picks the default described above.

## Gotchas

- The task project is built in Release before every use (target `BuildRinShadeMSBuildTask`), which is a quick no-op when nothing changed. `RinShade.targets` loads it with `TaskFactory="TaskHostFactory"`, so it runs in a process that exits when the task finishes and the DLLs are never left locked. An IDE that loaded the task before this was in place still holds the old lock until it is restarted.
- The on-demand build does not restore. On a clean checkout restore the solution first, as the CI workflow does (`dotnet restore rin.sln`), or the build fails with NETSDK1004.
- Compiling needs the real native Slang library.
- A reused MSBuild node keeps the task assembly it first loaded, so after the task's parameters change a build can fail with MSB4064 until the nodes exit (`dotnet build-server shutdown`, or build with `-nodeReuse:false`).
