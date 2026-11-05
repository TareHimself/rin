# Architecture

A bird's-eye map of Rin: what the layers are, how a frame moves through them, and the rules the pieces rely on. Each folder and project has a `ReadMe.md` with the detail. This file says how they fit together.

## Layers

```mermaid
flowchart BT
    subgraph native["native/ (C++, shipped as NuGet packages)"]
        N1["Rin.Native<br/>platform, SDF, video"]
        N2["Rin.Graphics.Vulkan.Native<br/>Vulkan, windowing, allocator"]
        N3["Rin.Audio.Miniaudio.Native"]
        N4["Rin.Slang.Native<br/>prebuilt Slang plus a small wrapper"]
    end

    subgraph tools["Shader toolchain"]
        SL["Slang/<br/>Rin.Slang.Compiler"]
        SH["Shade/<br/>C# to Slang transpiler,<br/>source generator, MSBuild task"]
    end

    subgraph engine["Engine/"]
        CORE["Rin.Core<br/>views, graphics and audio interfaces,<br/>shared types"]
        VK["Rin.Graphics.Vulkan"]
        GN["Rin.Graphics.Null"]
        AUD["Rin.Audio.Miniaudio / Rin.Audio.Null"]
        WORLD["Rin.World<br/>ECS world, render and physics features,<br/>default render pipeline"]
        GLTF["Rin.GLTF"]
    end

    EX["Examples/ and Experiments/"]

    N4 --> SL
    SL --> SH
    SH -. "builds shaders for" .-> CORE
    SH -. "builds shaders for" .-> WORLD
    N1 --> CORE
    N2 --> VK
    N3 --> AUD
    CORE --> VK
    CORE --> GN
    CORE --> AUD
    CORE --> WORLD
    CORE --> GLTF
    VK --> EX
    AUD --> EX
    WORLD --> EX
    GLTF --> EX
```

Arrows point from a dependency to the thing that uses it. The rule that holds the engine together: **`Rin.Core` defines the interfaces and the backends implement them.** `IGraphicsModule` and `IAudioModule` live in `Rin.Core`, `Rin.Graphics.Vulkan` and `Rin.Audio.Miniaudio` implement them, and the `Null` backends implement them with no-ops. An application picks its backends. The `headless` example (`Examples/Examples/HeadlessTest`) runs a physics world on the null backends, and most examples use Vulkan and Miniaudio through `Examples.Common`.

## From input to pixels

```mermaid
flowchart LR
    OS["Window event<br/>(native layer)"] --> RE["Resolve<br/>cursor position and text, the only native reads"]
    RE --> RW["RinWindow.ProcessEvent"]
    RW --> WS["WindowSurface"]
    WS --> V["View.HandleEvent<br/>hit test, OnCursorDown and friends"]
    V --> CH["A view changes state"]
    CH --> LY["Layout"]
    LY --> CO["Collect<br/>views append to a CommandList"]
    CO --> GR["Render graph<br/>passes from the command list"]
    GR --> GPU["Vulkan: compile graph,<br/>record, submit, present"]
```

1. The native window layer produces events. The Vulkan backend pumps them each update, resolves the cursor position and event text each one needs (`ResolvedWindowEvent.Resolve`), and routes them by window to `RinWindow.ProcessEvent`, which makes no native calls. `WindowSurface` turns them into surface events.
2. The `View` tree hit-tests the event, from children to parent, and views react (`OnCursorDown`, `OnScroll`, and so on).
3. Views lay themselves out, then `Collect` walks the tree and appends quads, text, textures, blur and clip commands to a `CommandList`.
4. The `CommandList` becomes render-graph passes: stencil passes for clipping, then draw passes whose commands are merged into batches. Details are in [Engine/Rin.Core/ReadMe.md](Engine/Rin.Core/ReadMe.md).

## One frame

```mermaid
sequenceDiagram
    participant M as Main thread
    participant R as Render thread
    participant G as Graph (Vulkan backend)

    M->>M: OnUpdate: lay out views, update worlds
    M->>M: OnCollect: surfaces add collected data
    M->>R: hand over render data
    R->>G: build passes from the collected data
    R->>G: Compile: configure passes, order, barriers
    R->>G: Execute passes, submit, present
    Note over M,R: the main thread starts the next update while the render thread runs
```

The render thread runs `IGraphicsModule.Execute` once per collected frame. Every renderer, so every open window, adds its passes to the same graph, and the frame ends in one submit and one present that carries every swapchain. `SetMaxFramerate` caps how often, which an application that is mostly still can use to keep the GPU quiet.

A **pass** declares the images and buffers it reads and writes in `Configure`, and records GPU work in `Execute`. The graph orders passes from those declarations, prunes passes nothing terminal depends on, adds upload and copy passes and inserts the barriers. The interfaces are in `Rin.Core` (`IPass`, `IGraphConfig`, `IGraphBuilder`), and the implementation is in `Rin.Graphics.Vulkan`. See [Engine/Rin.Graphics.Vulkan/ReadMe.md](Engine/Rin.Graphics.Vulkan/ReadMe.md).

## Worlds on a surface

`Rin.World` models a world as an ECS: a `World` of entities with struct components, and ordered systems that run on a fixed tick and then per frame. The world does not draw or simulate by itself. Features connect it to the backends through component hooks and systems:

```mermaid
flowchart LR
    APP["OnUpdate"] --> U["World.Update(dt)"]
    subgraph TICK["each fixed tick"]
        SC["ScriptFeature<br/>scripts, then GameTasks"] --> PH["PhysicsFeature<br/>push, step, pull, contacts"]
    end
    U --> TICK
    TICK --> FR["frame: animation, sockets,<br/>transform propagation"]
    FR --> EX["RenderFeature: RenderExtract"]
    PH <--> BEPU["IPhysicsSystem<br/>(BepuPhysicsSystem)"]
    EX --> RS["IRenderSystem proxies<br/>(DefaultRenderSystem)"]
    RS -- "Snapshot per camera" --> VP["Viewport"]
    VP --> GRAPH["render graph passes"]
```

Adding a `StaticMesh`, `SkinnedMesh` or light creates a render proxy and adding a `RigidBody` creates a physics body, both in `OnAdd` hooks. Physics writes simulated poses back into `LocalTransform` every tick, and once per frame `RenderExtract` pushes changed transforms, lights and poses to the render system. The renderer takes a **snapshot** of its proxies, blended by the world's interpolation alpha, and the default pipeline turns it into passes: skinning, culling, indirect draw preparation (or plain per-mesh draws on devices without indirect rendering), a G-buffer fill, and a lighting pass. A `Viewport` view renders a camera entity and its command handler puts the output on a surface. For lockstep and rollback, `World.SaveSnapshot` and `RestoreSnapshot` save and load the simulation state; the features register as restore listeners and rebuild their proxies, bodies and script bindings after a restore, since those hold handles into the backends. See [Engine/Rin.World/ReadMe.md](Engine/Rin.World/ReadMe.md).

## Shaders

Shaders are C# classes. `Rin.Shade` transpiles them to Slang during the build, `Rin.Slang.Compiler` compiles that to a package with SPIR-V and reflection, and the package is embedded as a resource named after the shader class's full name plus `.crsh`. At run time the backend opens it through the generated `Descriptor` on the class, which also carries the pipeline state (attachment formats, blend, depth and stencil). There are no hand-written `.slang` files in the repo. See [Shade/ReadMe.md](Shade/ReadMe.md).

## Rules the code relies on

- **Matrices are row-major with row-vector math** everywhere, matching `System.Numerics`. The CPU tests in `Shade/Rin.Shade.CpuTests` check that the Slang side agrees.
- **Every block uses scalar layout.** Push constants, `BufferRef<T>` pointees and the data in a `ParameterBlock` all follow scalar layout, which is also what C# sequential layout gives the same struct. The native Slang session forces it for the SPIR-V target, so no declaration can opt out. Shaders reach buffers through `BufferRef<T>` addresses passed as push constants.
- **Resources are bindless.** Shaders sample textures through the engine-owned `rin.global` block with a `DeviceHandle`. The engine binds that descriptor set once per frame.
- **Handles are generational.** A `ResourceHandle` carries a generation so a freed and reused slot is detected.
- **Zero-sized images and buffers are rejected** at the create calls, with an exception.
- **No allocation in per-frame, per-draw or per-item paths.** Build-time tooling is exempt. See [AGENTS.md](AGENTS.md).

## Native libraries and CI

A native library reaches the managed code in one of three ways: a **real** package built from C++ (locally with `uv run python scripts/pack_native.py`, in CI for Slang), a **stub** package that only satisfies restore, or a **fake** NativeAOT library that tests load instead. [native/ReadMe.md](native/ReadMe.md) explains each, and CI (`.github/workflows/ci.yml`) uses all three: stubs and fakes for the per-project test jobs, and the real Slang for the CPU shader tests and the full shader compile.

## Where to read next

- [ReadMe.md](ReadMe.md): setup, build and test commands, and the index of every ReadMe
- [Shade/ReadMe.md](Shade/ReadMe.md): the shader pipeline in depth
- [Engine/Rin.Core/ReadMe.md](Engine/Rin.Core/ReadMe.md): views and the graphics graph in depth
