# Rin.Shade conventions

Rules for changing the shader transpiler, its generators and the shaders written with it. They add to the root `AGENTS.md`. How the pieces work is in `ReadMe.md`.

## After changing the transpiler

- `Rin.Shade.MSBuild` is brought up to date, in Release, before every shader compile (`BuildRinShadeMSBuildTask` in `msbuild/RinShade.targets`), so a change to the transpiler or the task takes effect on the next build. The task runs in a short-lived task host, so no build process keeps its DLLs locked.
- The text snapshots do not prove a compiled shader is right. After changing lowering, build with `-p:RinShadeDumpGenerated=true` and read the emitted Slang in the output directory's `GeneratedShaders` folder.

## Tests

- `Rin.Shade.Tests` compares emitted Slang with snapshot files. After an intended emitter change, run the tests with `RIN_SHADE_SNAPSHOTS=update`, then read every changed snapshot before committing. A `.received.slang` file beside a snapshot is a failing comparison.
- `Rin.Shade.CpuTests` runs transpiled shaders on the CPU against `System.Numerics` and needs the real native Slang. It is what pins the matrix convention (row-major storage, row-vector math). Run it when you change matrix or vector lowering.
- Add a test for each new construct the transpiler learns, and a diagnostic test for each construct it rejects.

## Writing shaders

- Reuse the real engine types. Do not write `Shade*` mirror structs. Mark a type `[ShadeExport]` if other assemblies' shaders need it, and let the shader own a nested `PushConstants`.
- Semantic attributes (`[Position]`, `[VertexId]`, ...) are valid only on struct fields. Entry points take and return structs.
- Hand-written `.slang` files do not exist in this repo. Change the C#, not generated Slang.
- Shared behavior goes in a base class (generic bases are fine). Check the emitted Slang to confirm the chain flattened the way you expect.

## Docs

- Update `Shade/ReadMe.md` when the pipeline changes, and the matching project `ReadMe.md` when a project's surface changes. Any example in the docs must be real output, for example from `rin-shade compile`.
