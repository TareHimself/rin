# Animation system

Status doc for the layered/AnimGraph-style animation system being built in `Rin.World.Mesh.Skinning`
(and `Rin.GLTF` for content import). Last updated 2026-09-19.

## Current state

### Phase 0 — index-based skeleton/pose foundation (done)

- `Skeleton` (`Engine\Rin.World\Mesh\Skinning\Skeleton.cs`) is index-based: `BoneNameToIndex`
  (name → index, for binding/import only) and `ParentIndices` (parallel `int[]`, -1 for root).
  `ResolvePose` walks the parent chain by index, no per-frame dictionary lookups.
- Fixed a real bug along the way: an overridden bone used to lose its parent's transform entirely
  (`local * override`, dropping the parent chain). Now correctly composes as
  `(override or bind-local) * parentAbsolute`.
- `Pose` split into two types:
  - `SkeletalPose` — plain, GC-owned `Transform[]` + `ulong[]` mask. Safe to copy/store freely
    (render proxy table, `Skeleton.BasePose`, anything long-lived).
  - `PooledSkeletalPose` — same shape, backed by `MemoryPool<T>`-rented `Memory<T>`, `IDisposable`.
    Only for short-lived, single-threaded graph composition. **Copies alias the same buffer** —
    disposing one copy invalidates every other; dispose exactly once, right after a pose is
    consumed (blended into a parent, or materialized via `ToSkeletalPose()`).
- `AnimationClip` (name-keyed authoring, shareable across skeletons) / `BoundAnimationClip`
  (index-bound per skeleton via `AnimationClip.Bind`, zero string lookups at sample time).
- Closed a real end-to-end gap: `SkinnedMeshComponent`'s render proxy used to sample
  `PoseSource.GetPose()` once in `Start()` and never again — any pose source was frozen forever.
  Added `IRenderSystem.UpdateSkinnedProxyPose`, wired through `SkinnedMeshComponent.LateUpdate`.

### Phase 1 — `Rin.GLTF` (done)

- `GltfMeshImporter` (moved/generalized from `Examples\SceneTest\Extensions.cs`) and
  `GltfAnimationImporter` (new): imports glTF animations into `AnimationClip`, mapping
  STEP/LINEAR channels directly onto `BoneCurve`. CUBICSPLINE is approximated as linear
  (`BoneCurve`'s "cubic" tangent is a scalar ease shape, not glTF's per-axis Hermite derivative
  vectors — a faithful conversion isn't possible).
- No implicit root bone: bones come only from `skin.joints` (true joints per the glTF spec), never
  a wrapping "Armature" scene-graph node — avoids the classic UE/Blender-FBX extra-root-bone issue.
- Found and fixed a real, pre-existing bug in `Curve<T,V>.NearestIndex`
  (`Engine\Rin.Core\Shared\Curves\Curve.cs`): the binary-search midpoint was
  `maxIdx + (maxIdx-minIdx)/2` instead of `minIdx + (maxIdx-minIdx)/2`, walking the probe index
  outward past the array bounds. Crashed on any curve with more than a couple of keyframes at
  certain sample times; every prior test only ever used 1-2 keyframes so it was never exercised
  until the real Khronos "Fox" sample asset's animation data hit it.

### Phase 2 — AnimGraph (done; montages/slots descoped)

`Engine\Rin.World\Mesh\Skinning\Animation\`:

- `IPoseNode` — `PooledSkeletalPose Evaluate(in AnimationEvalContext ctx)`.
- `ClipPlayerNode` — advances/loops a `BoundAnimationClip` by `Rate`, wraps at `Duration`.
- `BlendNode` — two-way blend by `Weight`; evaluates and disposes both children every tick.
- `StateMachineNode` / `AnimationState` / `AnimationTransition` — linear current→target crossfade
  driven by a `Func<bool>` condition and a blend duration. Both branches evaluate during a
  transition (needed for the crossfade, and — see Notifies below — this is also what makes
  notify firing "just work" across blends with no extra design).
- `AnimationGraph` — implements `IPoseSource`. Meant to be subclassed per character (mirrors UE's
  `UAnimInstance`): put gameplay "blackboard" fields (`Speed`, `IsCrouching`, ...) directly on a
  derived class and close over `this` in `AnimationTransition.Condition` delegates. `Tick(dt)`
  evaluates the root node and materializes the pooled result into a plain `SkeletalPose`
  (`ToSkeletalPose()`) — this is the pooled→plain boundary crossing.
- `IPoseSource.Tick(float)` (default no-op) + `SkinnedMeshComponent.Update` calling
  `PoseSource?.Tick(deltaSeconds)`, `LateUpdate` still doing the push. `Tick`/`LateUpdate` run once
  per `World.Update()` call with **variable**, real-frame `deltaSeconds` (only physics sub-steps at
  a fixed rate) — animation playback speed is currently frame-time-based, not fixed-step. No
  concrete need for fixed-step animation has shown up yet (no networking/replay requirement, no
  root motion, no frame-exact hit windows) — revisit if one does.
- Point notifies exist (see below); notify **states** (Begin/End) are designed but not built.

### Descoped / deferred

- **Montages, slots, layered (per-bone-masked) blending, additive blending.** Real montages are
  actually two separable AnimGraph concepts in UE (a full-body "Slot" blend node, and a separate
  "layered blend per bone" node usually composed alongside it), plus a standalone timeline asset
  with sections/notifies — substantial scope on its own. Base graph (clips, blending, state
  machines) was judged more valuable to land first.
- **Resolved bone-transform / socket queries on `SkinnedMeshComponent`**, and a **socket/attachment
  system** (things parented to a bone/socket, invalidated when the pose re-resolves). Surfaced as a
  prerequisite for any notify handler that wants to do more than fire-and-forget (e.g. spawn an
  effect at a bone's current position) — not scoped or built yet. Real complexity: `ResolvePose`
  today only happens deep in `SkinningPass` on the (decoupled, possibly-a-frame-behind) render
  side; game-side queries would need `SkinnedMeshComponent` to resolve its own pose too, which is
  a real second resolve pass, not free.

## Notifies — design

### Detection: interval-crossing, not point-sampling

A notify's timestamp is checked against `[previousTime, rawTime)` — the interval clip-local time
advanced through this tick — not sampled as a point. This means a hitch/frame-drop doesn't cause a
missed notify by itself; the only real edge case is a **looping** clip wrapping more than once in a
single tick (huge hitch + a very short clip), which isn't handled (single-wrap only) and is an
accepted rare edge case, not a correctness goal.

### Layered/blended evaluation: free, no extra design

Because `BlendNode`/`StateMachineNode` always fully evaluate every child every tick (needed for the
blend itself), notify detection naturally happens at the leaf (`ClipPlayerNode`) level, independent
of blend weight. A notify from a clip blended at 1% weight still fires exactly as if it were the
only thing playing — matching UE's default behavior (notifies are usually gameplay-meaningful
events tied to logical progress, not something that should silently drop based on blend timing). A
later opt-in "trigger weight threshold" (suppress firing below some effective weight) would need an
ambient-weight value threaded down through `AnimationEvalContext` as blend nodes descend — not
built, not needed until a concrete "ghost notify from a barely-blended-in animation" problem shows up.

### Dispatch: collected during traversal, invoked centrally, after pose resolve

Mirrors UE's actual split (`AnimInstanceProxy` notify queue during Update, invoked once after):

1. Leaf nodes report crossings into a per-tick sink carried on `AnimationEvalContext`
   (`FiredNotifies`, currently `List<string>` — see below for the typed replacement).
2. `AnimationGraph.Tick()` owns the sink, clears/repopulates it each tick, exposes what fired via
   `FiredNotifies` afterward.
3. **Actual invocation happens in `SkinnedMeshComponent`**, not in the graph/node layer. Two
   reasons: (a) handlers need real gameplay context (the mesh component, eventually bone/socket
   transforms) that `Rin.World.Mesh.Skinning` types don't and shouldn't hold; (b) handlers need
   pose-*resolved* state, and resolution doesn't happen in the graph at all today (see "resolved
   bone-transform" gap above). `SkinnedMeshComponent.Update` calls `Tick`, then (once resolve
   access exists) resolves/dispatches, then `LateUpdate` pushes the render pose as before.

`IAnimationNotify`/`IAnimationNotifyState`/`SkinnedMeshComponent` living in the same `Rin.World`
assembly but different namespaces (`Mesh.Skinning` vs `Components`) is not a circular-reference
problem — that only applies across project/assembly boundaries, not namespaces within one project.

### Notify states: per-occurrence instances, not a shared handler + opaque token

Two shapes were considered:

- **Opaque token**: one shared stateless handler instance; `NotifyBegin` returns `object?`, graph
  stores it, hands it back to `NotifyEnd`. Works, but per-occurrence data has to be smuggled through
  an untyped token instead of being normal instance fields.
- **Per-occurrence instance** (chosen): each occurrence gets its own handler instance (via a
  factory - see below), so `NotifyBegin`/`NotifyEnd` are just ordinary instance methods reading and
  writing ordinary instance fields. Cleaner to author.

```csharp
public interface IPooledNotify { void Reset(); }
public interface IAnimationNotify : IPooledNotify
{
    void Notify(SkinnedMeshComponent meshComponent, AnimationGraph graph);
}
public interface IAnimationNotifyState : IPooledNotify
{
    void NotifyBegin(SkinnedMeshComponent meshComponent, AnimationGraph graph);
    void NotifyEnd(SkinnedMeshComponent meshComponent, AnimationGraph graph);
}
```

`Reset()` doubles as "prepare for use" and "clear before pooling" — this unifies stateless,
new-each-time, and pooled handlers into **one** generic pooled factory implementation: a stateless
handler just has an empty `Reset()`, so pooling it is free/harmless even though it never needed
pooling to begin with.

```csharp
public interface IAnimationNotifyFactory
{
    IAnimationNotify Create();
    void Release(IAnimationNotify instance);
}

public static class NotifyFactory<T> where T : class, IAnimationNotify, new()
{
    public static readonly IAnimationNotifyFactory Instance = new PooledAnimationNotifyFactory<T>();
}
```

The factory interface is **non-generic at the storage boundary** on purpose: `AnimationClip.Notifies`
needs one uniform list element type, and `Create`-returns-`T`/`Release`-accepts-`T` forces
`IAnimationNotifyFactory<T>` to be invariant (can't substitute `IAnimationNotifyFactory<Footstep>`
for `IAnimationNotifyFactory<IAnimationNotify>`), so the generic convenience (`NotifyFactory<T>`)
wraps the non-generic interface instead of being the stored type itself.

Considered and rejected (for now): a source generator (matching the existing `[ComputeShader]`/
`[GraphicsShader]` precedent in `Rin.SourceGenerators`) that would inject a `FootstepNotify.Factory`
static member directly onto the notify class. `NotifyFactory<FootstepNotify>.Instance` gets the same
zero-boilerplate result via plain generics, no build-time codegen to maintain. Worth reconsidering
only if the `TypeName.Factory` call-site syntax specifically matters enough to justify it.

### Begin/End firing: diffed at the graph level, not tracked per-node

Instead of each `ClipPlayerNode` remembering "did I already fire Begin" and needing an explicit
stop/interrupt signal for correctness (a `IPoseNode.Stop()` every composite node would need to
propagate correctly), the leaf just reports a **pure, memory-free fact each tick**: "given my
current `Time`, here are the notify states I'm currently inside." `AnimationGraph` is the only
thing with cross-tick memory, so it owns the actual decision:

- key present this tick, not last tick → fire `Begin` (via `factory.Create()` + `NotifyBegin`)
- key present both ticks → nothing (already began)
- key present last tick, not this tick → fire `End`, then `factory.Release(instance)` —
  **regardless of why it disappeared**

That last line is what makes interruption correct for free: a state notify's window ending
naturally, and a state-machine transition swapping the node away mid-window, look identical to the
graph (the key just stops being reported as active) — both correctly fire `End` exactly once, with
no lifecycle/stop propagation needed anywhere else. Key identity: reference-equality on the
authored `AnimationNotify`/state-range object (not structural equality), so two different clips that
happen to share a name/time don't collide.

## Future: editor-driven composition

Not building this now — no visual editor infra exists anywhere in Rin yet, and there's no concrete
timeline for one — but worth naming what today's code-first design would and wouldn't survive, so
current choices don't accidentally foreclose it.

The editor still needs its own data/asset format regardless (what actually gets saved, diffed, and
reopened) - the open question is how that saved data turns into running code. Two shapes:

- **Runtime interpretation**: a builder walks the saved data at load time and constructs the actual
  `IPoseNode` graph, with `Condition` evaluated by a small data-driven expression interpreter
  (blackboard key vs. constant, etc.) instead of a compiled delegate.
- **Codegen** (favored - matches this repo's existing pattern): a build step compiles the saved
  graph asset into a generated `AnimationGraph` subclass, the same shape `Rin.SourceGenerators`
  already uses for `[ComputeShader]`/`[GraphicsShader]`, and the same shape the existing `.slang`
  shader pipeline already uses more broadly (`RinSlang.targets` - author content in one format,
  compile it into something consumed at build/runtime). Generated code can emit real
  `Func<bool>` lambdas (`Condition = () => Speed > 0.1f;`) exactly like hand-written code, so
  `AnimationTransition.Condition` needs **no rework at all** under this path - the compiled-delegate
  shape survives unchanged. Cost: no hot-reload on structural graph edits (new nodes, rewired
  connections) without a recompile, unlike pure interpretation.

Everything else already named compatible above holds under either path:
- `IPoseNode` types (`ClipPlayerNode`/`BlendNode`/`StateMachineNode`) are reasonable runtime
  primitives regardless of authoring method.
- `AnimationGraph` subclassing for blackboard fields (`Speed`, `IsCrouching`, ...) is already
  reflection-friendly - public fields are discoverable without redesign.
- Notify factories (`NotifyFactory<T>`) just need a registry/reflection layer later (enumerate
  types implementing `IAnimationNotify`, list them in a picker) - additive, not a rework.

## Open questions / not yet decided

1. **Resolved bone-transform + socket/attachment system.** Real scope on its own (game-thread pose
   resolve separate from the render-side one in `SkinningPass`, a socket concept, and a
   `TransformVersion`-style invalidation path for things attached to a bone). Needed before notify
   handlers can do anything beyond fire-and-forget. Not scoped yet — build now as part of notifies,
   or land notify dispatch with handlers limited to `(meshComponent, graph)` context first and treat
   this as its own follow-up pass?
2. **`FootstepNotify.Factory`-on-the-class syntax via source-gen** — worth it, or is
   `NotifyFactory<FootstepNotify>.Instance` good enough?
3. Notify **states** are fully designed above but not implemented — `AnimationClip.NotifyStates`,
   the factory types, and the graph-level active-set diffing all still need writing.
