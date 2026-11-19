# Rin.World

The scene layer on top of [Rin.Core](../Rin.Core/ReadMe.md): an archetype ECS (`World`) with a fixed tick, plus features that connect it to rendering, physics and scripts.

```
dotnet build Engine/Rin.World/Rin.World.csproj
dotnet test Engine/Rin.World.Tests/Rin.World.Tests.csproj -p:RinShadeSkipCompile=true
```

Without `RinShadeSkipCompile=true` the build also compiles shaders and needs the real Slang native package.

```mermaid
flowchart LR
    G["gameplay: systems, scripts, tasks"] --> W["World"]
    RF["RenderFeature"] --> W
    RF --> RS["DefaultRenderSystem"]
    PF["PhysicsFeature"] --> W
    PF --> PS["BepuPhysicsSystem"]
    SF["ScriptFeature"] --> W
    V["Viewport"] --> RF
```

The ECS runs on Friflo.Engine.ECS, but only `Ecs/` may reference it (a banned-API build error elsewhere), so gameplay code sees only Rin types.

**NativeAOT:** a project that declares components references [Rin.SourceGenerators](../Rin.SourceGenerators/ReadMe.md) as an analyzer, which registers them at startup. Generic or private components get warning `RIN00024` and need `ComponentRegistry.RegisterComponent<T>()` by hand.

## Entities and the update step

| Piece | What it is |
| --- | --- |
| `Entity` | Id plus generation, stale after despawn |
| `Query<T1..T4>` | `foreach` gives one `Span<T>` per component. Structural changes inside the loop throw, so use `world.Commands` |
| Hierarchy | `SetParent` and friends. Despawn removes descendants too |
| Hooks | `OnAdd<T>`, `OnRemove<T>`. Features create and destroy proxies and bodies from them |
| Transforms | `LocalTransform` (setters mark it dirty) and `WorldTransform`, propagated once per frame |

`World.Update(dt)` runs fixed ticks (1/60 s by default), then the frame phases:

```mermaid
flowchart LR
    T["each tick: Input, Gameplay, Tasks, Physics, play back Commands"] --> F["each frame: Update, Late, Extract"]
```

## Scripts and tasks

`new ScriptFeature(world)`, then `scripts.Attach(entity, script)`. A `Script` has `Start`, `Tick`, `Stop` and `Restore`. An entity can hold several, run in order. `GameTask` methods are `async` on the tick, not the thread pool. From SceneTest:

```csharp
public sealed class BoxRain(float intervalSeconds, Action dropBox) : Script
{
    protected override void Start() => Tasks.Run(Rain);

    protected override void Restore() => Tasks.Run(Rain);

    private async GameTask Rain(TaskContext ctx)
    {
        while (true)
        {
            await ctx.Seconds(intervalSeconds);
            dropBox();
        }
    }
}
```

- `TaskContext` also has `Ticks`, `NextTick`, `Until`, `Event<T>`, `WhenAny` and `WhenAll`.
- A cancel throws `GameTaskCanceledException` at the pending `await`, so `catch` and `finally` run. `CancelReason` says why.
- Awaiting a regular `Task` faults the task.
- Only components survive a snapshot restore. `Restore` restarts tasks from them.

The `scripting` example has one small script per pattern (patrol, sequenced door, fuse and defuse, cancellation).

## Rendering

`new RenderFeature(world, renderSystem, meshFactory)`. Components hold only ids and handles.

| Component | What it does |
| --- | --- |
| `StaticMesh`, `SkinnedMesh` | A `MeshHandle` from `IMeshFactory.CreateMesh` plus a `MaterialSet` from `RenderFeature.Materials`. The mesh is drawable as soon as `CreateMesh` returns. Skinned meshes also need a pose source |
| `PointLight`, `DirectionalLight` | Color and brightness in physical units: `Lumens` for a point light (a household bulb is about 800), `Lux` for a directional light (direct sun is about 100,000). A point light's optional `Radius` caps its reach; unset, it reaches until it gives less than 0.01 lux |
| `EnvironmentLight` | Lights the scene from an environment map: a handle from `RenderFeature.Environments`, an intensity, and an optional box (`Extent`, `BlendDistance`, `Priority`) |
| `Camera` | `world.GetCameraView(entity)` for a `Viewport`. `Ev100` is the exposure in photographic stops (about 15 for direct sun, 5 to 9 indoors, higher is darker) and `ToneMapCurve` (`AgX` by default, `Neutral`, `Aces`) picks how it is fitted to the display |

One mesh or light per entity, so use child entities for more. Animation runs per frame. `renderFeature.AttachToBone(child, parent, "bone")` makes a child follow a bone, with its `LocalTransform` as the offset from the bone. Call it again to move it to another bone, and `DetachFromBone` to stop.

### Environment lighting

```csharp
var sky = EnvironmentMap.FromRadiance(File.ReadAllBytes("sky.hdr"));
world.Add(world.Spawn(), new EnvironmentLight { Map = renderFeature.Environments.Create(sky) });
```

`EnvironmentMap` prepares a panorama on the CPU: it shrinks it to what the cubemap can show and works out spherical harmonics for diffuse light. `Environments.Create` uploads the panorama, and the first frame that uses the map runs `EnvironmentFilterPass` once, which draws every mip of every cubemap face from it, each mip blurred for a rougher surface.

| `Extent` | Reach |
| --- | --- |
| Zero | Everywhere, like a sky. The highest `Priority` wins |
| A box half-size | Pixels inside the box around the entity, fading in over `BlendDistance` from its border and blending with the unbounded light underneath. The highest `Priority` wins an overlap, and equal priorities cross-fade |

Surfaces get diffuse light and reflections, blurrier the rougher they are. Each pixel finds the boxes it is inside through the light grid, and a box's reflections are corrected as if its map were painted on the box's walls. The sky pass draws the unbounded map behind the scene, blended towards the box the camera is inside. Reflections show only the maps, so objects do not appear in each other.

### Materials

A material is a struct whose fields are its parameters, with a static `Evaluate` that returns the `Surface` at a pixel. Its shader is one line, and `SurfaceMaterial<T>` draws it. From the engine and SceneTest:

```csharp
[Shader]
public partial class PbrShader : SurfaceShader<PbrMaterial>;

return new SurfaceMaterial<PbrMaterial>(new PbrMaterial
{
    BaseColorTexture = albedo.Result,
    RoughnessTexture = roughness.Result,
    MetallicTexture = metallic.Result
});
```

- Fields: scalars, vectors, matrices, fixed-size arrays and `DeviceHandle` textures. Report textures from `GetTextures`.
- To move vertices, override `ProcessVertex`, set `MaxVertexOffset`, and return a `SurfaceDepthShader<T>` from `DepthShader`, or the depth prepass won't match. `Examples/Examples/SceneTest/WavyMaterial.cs` is a complete one.
- Anything that needs its own passes implements `IMeshMaterial` directly.

## Physics

`new PhysicsFeature(world, physicsSystem)`, then add a `RigidBody` (shape, `PhysicsState`, mass, trigger) and optionally `Velocity`.

| `PhysicsState` | Behaviour |
| --- | --- |
| `Static` | Never moves |
| `Controlled` | Follows the entity's transform every tick |
| `Simulated` | Bepu moves it and writes the pose back to `LocalTransform` |

`RigidBody` is read once, so remove and re-add it to change the shape. `PhysicsFeature.Contacts` holds the last tick's contacts. Casts and overlaps return entities.

## Determinism and snapshots

The tick is deterministic on the same build and CPU family. Tick code must:

- use `TickContext.DeltaSeconds`, never the frame delta
- never iterate a `Dictionary` or `HashSet`
- spawn and despawn through `Commands`
- draw random numbers from `world.Random`
- keep components unmanaged (managed data goes in a registry, and the component holds an id)

`world.SaveSnapshot(snapshot)` and `RestoreSnapshot(snapshot)` rewind the world, and `snapshot.ComputeHash()` compares worlds. Render proxies, physics bodies and scripts are rebuilt after a restore. Running tasks and pending commands are dropped. Bepu's internal state is not restored, so a restored physics run only matches another run that restored at the same ticks.

## The render pipeline

`DefaultRenderSystem` queues proxy changes and rebuilds its snapshot when something changed. Each camera then adds these passes:

```mermaid
flowchart LR
    S["skinning, bounds, scene data"] --> C["culling, indirect commands"] --> D["depth prepass"] --> LG["light grid"] --> F["forward color"] --> K["sky"] --> T["tone map"]
```

The light grid culls everything with a bounded reach: point lights by their `Radius` and environment boxes by a sphere around them. `ClusterBinner` sorts these items by kind and then by view depth, and fills 1024 depth bins per kind on the CPU. A compute pass then marks which items touch each 32 x 32 pixel tile. Each item says which kind it is and which entry of that kind's data array it stands for, so adding a kind is one more array and one more loop. The forward pass draws every mesh again with its material's shader, which evaluates the surface and lights it right there, looping only the items in both the pixel's tile and its depth bin, plus every directional light and the unbounded environment. There is no G-buffer.

Draws are batched by index buffer plus material pass, and materials that don't move vertices share one depth pass. Without indirect rendering (or with `DrawMode = Direct`), culling is skipped and meshes draw from the CPU. Culling doesn't cull anything yet.

`Viewport` shows a world in a Rin.Core surface. A click cycles its `RenderChannel`: the lit scene, the surface's color, location, normal, roughness/metallic/specular and emissive, the incoming radiance, and a heatmap of how many lights each pixel loops. The channel travels in the `CameraView`, and the surface shader picks it at the end of its fragment.

References: Rin.Core, Rin.Shade, Rin.SourceGenerators. Packages: BepuPhysics, Friflo.Engine.ECS.
