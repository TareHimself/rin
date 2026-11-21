# Rin.World.Benchmarks

BenchmarkDotNet suite for the ECS, written against the public `World` API only so the same cases can compare ECS backends.

```
dotnet run -c Release --project Engine/Rin.World.Benchmarks -- --filter *
```

Set `RinShadeSkipCompile=true` in the environment first: BenchmarkDotNet rebuilds the project in its own output folder, where the shader build step cannot run. Add `--job short` for a quick pass. Every case runs at 10k and 100k entities.

| Class | Cases |
| --- | --- |
| `EntityBenchmarks` | spawn with 3 components, add then remove one component, random `Get` and `Has` |
| `QueryBenchmarks` | iterate 1, 2 and 4 components, writing one |
| `CommandBufferBenchmarks` | record and play back an add, then a remove, per entity |
| `DespawnBenchmarks` | despawn every entity |
| `SnapshotBenchmarks` | `SaveSnapshot` and `RestoreSnapshot` |
| `ChangedQueryBenchmarks` | a frame system reading a `Changed` query against one reading every row, with nothing written and with 1% of rows written per frame |
| `FrameExtractBenchmarks` | one `Update` of transform propagation plus render extract over static meshes on a null render backend, still and with 1% moving |

Add and remove are timed as a pair because the entities must return to their archetype for the next run.

References: Rin.World, Rin.SourceGenerators (as an analyzer, to register the components).
