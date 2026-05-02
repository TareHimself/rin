# Rendering System Deep Dive

This document maps the current rendering system end-to-end, from application threading to Vulkan command submission and the Views/UI frame-graph pipeline.

## Scope and Baseline

- This describes the **current active runtime path** centered on:
  - `Rin.Framework`
  - `Rin.Framework.Graphics.Vulkan`
  - `Rin.Framework.Views`
- Legacy/migration projects (`Rin.Engine.World`, parts of `misc.*`, `Rin.Editor`) are not treated as source-of-truth for rendering architecture.

---

## 1) Top-Level Architecture

The rendering stack is layered:

1. **Application lifecycle + threading**
   - `Rin.Framework/Application.cs`
2. **Graphics module abstraction + backend**
   - API: `Rin.Framework/Graphics/IGraphicsModule.cs`
   - Backend: `Rin.Framework.Graphics.Vulkan/VulkanGraphicsModule.cs`
3. **Renderer abstraction**
   - `IRenderer` + `IWindowRenderer`
   - Core implementation: `WindowRenderer`
4. **Frame graph**
   - Build/config/compile/execute path in `Rin.Framework.Graphics.Vulkan/Graph/*`
5. **Views/UI rendering pipeline**
   - `ViewsModule` + `WindowSurface` + command handlers/passes in `Rin.Framework/Views/Graphics/*`
6. **Vulkan execution context + descriptors/shaders/resources**
   - `VulkanExecutionContext`
   - Slang shader manager and bind contexts
   - Resource pooling and bindless image factory

---

## 2) Threading and Frame Timeline

### Ownership model

- Main update thread runs app/update/input collection.
- Render thread executes rendering.
- Synchronization uses `_renderFinishedEvent` and `_mainUpdateEvent` (`Application.Run`).

### Per-frame order (`Rin.Framework/Application.cs`)

Main thread:
1. `OnPreUpdate`
2. `MainDispatcher.DispatchPending()`
3. `OnUpdate(delta)`
4. `OnPostUpdate`
5. wait for render completion (`_renderFinishedEvent.WaitOne()`)
6. `OnCollect` (collect render data for next render thread step)
7. signal render thread (`_mainUpdateEvent.Set()`)

Render thread:
1. wait for collect signal (`_mainUpdateEvent.WaitOne()`)
2. `RenderDispatcher.DispatchPending()`
3. `OnPreRender`
4. `OnRender`
5. `OnPostRender`
6. signal main thread (`_renderFinishedEvent.Set()`)

### Graphics module event wiring

`VulkanGraphicsModule.Start` subscribes:
- `app.OnUpdate += Update`
- `app.OnCollect += Collect`
- `app.OnRender += Execute`

This creates the canonical flow:
- **Update**: pump native window events
- **Collect**: invoke renderer `Collect()` on main thread
- **Execute**: consume collected data on render thread

---

## 3) Core Graphics Module (`VulkanGraphicsModule`)

### Responsibilities

- Vulkan instance/device/queues initialization and shutdown
- Native window event pump (`Native.Platform.Window.*`)
- Window creation and lifecycle to `WindowRenderer`
- GPU resource creation APIs (buffers/textures/cubemaps)
- Queue submission helpers and async submit lanes

### Window-to-renderer registration

When a window is created (`HandleWindowCreated`):
- creates `WindowRenderer`
- calls `Init()`
- stores in `_windows`
- adds renderer to `_renderers`
- raises `OnWindowCreated` and `OnWindowRendererCreated`

### Collect / Execute contract

- `Collect()` snapshots `_renderers` and calls `renderer.Collect()`; non-null `IRenderData` is cached in `_collected`.
- `Execute()` flushes pending transfer/graphics submits, then calls `context.Renderer.Execute(context)` for each collected item.

This mirrors `IRenderer` design (`Collect()` returns immutable-ish frame data, `Execute()` consumes it).

---

## 4) Window Renderer and Swapchain (`WindowRenderer`)

`WindowRenderer` is the primary concrete `IWindowRenderer` for Vulkan windows.

### High-level `Execute(IRenderData)`

1. Validate render context (`RenderData`) and extent.
2. Recreate swapchain if needed (`_dirty` or extent change).
3. Build+compile+execute frame graph for this frame.
4. Submit command buffer, present swapchain image.

### Frame objects

- `Frame` owns:
  - command pool + primary command buffer
  - render fence
  - swapchain wait semaphore
  - per-frame descriptor allocator
- `Frame.WaitForLastDraw()` prevents reusing in-flight frame resources.

### Swapchain recreation path

- `MarkDirty()` flags recreate.
- `DestroySwapchain()` waits idle, destroys semaphores/views/swapchain.
- `CreateSwapchain()` builds swapchain, images, views based on present mode and surface capabilities.

### Per-frame graph execution (`DoExecute`)

1. Build graph with `GraphBuilder(_resourcePool, frame)`.
2. Collector writes passes (`ctx.Collector.Write(builder)`).
3. Force terminal state transition by appending `PrepareForPresentPass`.
4. Acquire swapchain image.
5. Add swapchain image as destination texture in graph.
6. Compile graph.
7. Begin command buffer.
8. Bind global bindless descriptors.
9. Execute graph with `VulkanExecutionContext`.
10. End command buffer.
11. Submit queue (`vkQueueSubmit2`) with semaphore choreography.
12. Present (`vkQueuePresentKHR`).
13. Mark frame finished and increment frame index.

---

## 5) Frame Graph Internals

### Core types

- Builder: `GraphBuilder`
- Config phase: `GraphConfig`
- Collector: `GraphCollector`
- Runtime object: `CompiledGraph`
- Pass abstraction: `IPass`

### Build phase

- Data sources (`ICollectedData`) emit passes into builder.
- Passes call `IGraphConfig` during `Configure()` to:
  - create resources
  - declare read/write usage + layouts/usages
  - add explicit pass dependencies if needed

### Compile phase (`GraphBuilder.Compile`)

1. Call `Configure()` on every pass.
2. Find terminal passes (`IsTerminal == true`); if none, graph is empty.
3. Backward traverse dependencies from terminals:
   - pass dependencies
   - read-after-write edges
   - write-after-previous-reads/writes edges
4. Prune unreachable passes (invoke `OnPrune` if set).
5. Derive resource barriers:
   - image layout transitions
   - buffer usage/operation transitions
6. Topologically levelize passes.
7. Inject `BarrierPass` execution groups before groups requiring sync.

### Execute phase

`CompiledGraph.Execute(context)`:
- lazily materializes resources from descriptors via `IResourcePool`
- executes execution groups in order
- each pass reads resources by id and issues API calls through `IExecutionContext`

### Important behavior

- Graph resources are ID-addressed.
- Resource allocation is lazy (on first `GetTexture/GetBuffer/...`).
- Barrier insertion is automatic from declared usage/layout transitions.
- `PrepareForPresentPass` ensures swapchain reaches `PresentSrc`.

---

## 6) Views Rendering Pipeline (UI)

### Module integration

- `ViewsModule` listens to `IGraphicsModule.OnWindowRendererCreated/Destroyed`.
- Creates one `WindowSurface` per `IWindowRenderer`.

### Surface collection hook

- `WindowSurface` subscribes to `Renderer.OnCollect`.
- On collect, surface builds `CommandList` from root view tree (`Surface.CollectCommands`).
- If commands exist, emits `WindowSurfaceCollectedData` into graph collector.

### View tree to draw commands

- `Surface.CollectCommands` calls `_rootView.Collect(...)`.
- `CompositeView.Collect` handles clipping and child traversal.
- `ContentView.CollectContent` writes draw commands.
- Geometry commands are mostly quads via `QuadExtensions`.

### CommandList model

- Stores ordered commands + parallel clip stack IDs.
- Maintains unique clip stack table (`UniqueClipStacks`).
- Clip stacks become stencil masks later.

### Converting commands to passes (`DefaultCollectedSurfaceData.Write`)

1. Add `CreateImagesPass` (main/copy/stencil images).
2. Iterate commands and clip IDs:
   - assign stencil masks per unique clip stack
   - emit `StencilWritePass` when a new stack appears
   - split batches when mask-space is exhausted (31-bit mask handling) and emit `StencilClearPass`
3. Group commands into `ViewsDrawPass` batches by:
   - pass config type
   - command handler type
   - explicit break via `NoOpCommand`
4. `WindowSurfaceCollectedData` appends `CopySurfaceToSwapchain`.

### Pass responsibilities

- `CreateImagesPass`
  - creates and clears main/copy/stencil images
- `StencilWritePass`
  - writes clip geometry to stencil with per-stack mask
- `ViewsDrawPass`
  - owns pass config lifecycle (`Begin/End`) and runs command handlers
- `StencilClearPass`
  - clears stencil when mask space is reset
- `CopySurfaceToSwapchain`
  - blits/copies main image into swapchain image

---

## 7) Pass Config + Command Handler Model

### `IPassConfig`

- Encapsulates render target usage and render-state setup for a command group.
- Example: `MainPassConfig`
  - config stage: writes main image as `ColorAttachment`, reads stencil as `StencilAttachment`
  - begin stage: starts rendering, disables culling, enables stencil compare
  - end stage: ends rendering

### `ICommandHandler`

- `Init(commands)` preprocesses command batch.
- `Configure(...)` declares graph resources.
- `Execute(...)` issues draw/compute commands.

### Batching path

- `BatchCommandHandler` aggregates `IBatchedCommand`s by batcher and stencil mask.
- Allocates one host-visible graphics buffer for packed batch data.
- During execute, sets stencil compare mask and calls batcher draw.

### Default quad path

- `QuadDrawCommand` -> `BatchCommandHandler` -> `DefaultQuadBatcher`.
- `DefaultQuadBatcher` binds `Framework/Shaders/Views/batch.slang`, writes quad buffer + push constants, then issues `Draw`.

---

## 8) Vulkan Execution Context

`VulkanExecutionContext` is the concrete bridge from pass logic to Vulkan commands.

Provides:
- image/buffer barriers
- buffer-image/image-image copies
- dynamic rendering begin/end
- depth/stencil state control
- clear operations
- draw/dispatch helpers (via bind contexts)

Command handlers and passes only depend on `IExecutionContext`, keeping render graph logic backend-agnostic at interface level.

---

## 9) Shader System (Slang -> SPIR-V)

### Manager

`SlangShaderManager`:
- owns one `SlangSession`
- caches shaders by absolute path
- compiles asynchronously through `BackgroundTaskQueue`

### Source loading

- Shader files are loaded through `SFramework.Sources`.
- `ImportFile` recursively resolves `#include "..."`.
- Source resolver supports:
  - filesystem (`fs/...`)
  - embedded framework content (`Framework/...`)

### Reflection-driven resource binding

During compile (`SlangGraphicsShader` / `SlangComputeShader`):
- parse reflection JSON
- infer descriptor bindings, sets, counts, stages, binding flags
- infer push constants
- build descriptor set layouts and pipeline layout
- create Vulkan pipeline

### Runtime binding

- `VulkanGraphicsBindContext` / `VulkanComputeBindContext` bind pipelines.
- `VulkanBindContext` manages descriptor set allocation/binding and deferred updates.
- Push constants are emitted directly via `vkCmdPushConstants`.

---

## 10) Resource Lifetime and Pooling

### Graph resource pool (`ResourcePool`)

- Pools textures, texture arrays, cubemaps, buffers by descriptor hash.
- Reuses resources across frames when not actively used by current frame.
- Uses proxy objects tied to `Frame` to return resources to pool on dispose.
- Evicts unused resources after `numFramesInFlight` grace period.

### Bindless image factory (`VulkanBindlessImageFactory`)

- Maintains giant descriptor set for sampled resources.
- Allocates stable `ImageHandle`s for texture/array/cubemap IDs.
- Supports sync and async creation paths.
- Writes default fallback resources for freed slots.
- Binds descriptor set globally for graphics and compute each frame.

---

## 11) Native and External Dependencies

- Vulkan interop: `TerraFX.Interop.Vulkan`
- Native Vulkan/platform bridge from `Rin.Framework.Graphics.Vulkan.NativeLibs`
- Framework native utilities from `Rin.Framework.NativeLibs`
- Slang integration through native layer exposed in `Native.Slang.*`

---

## 12) Extension Points for Contributors

### Add a new graph-driven renderer

1. Implement `IRenderer` or `IWindowRenderer`-adjacent collector source.
2. During `Collect()`, produce `ICollectedData` units.
3. In each `ICollectedData.Write`, add passes and resource declarations.

### Add a custom Views command type

1. Implement `ICommand` (or derive `TCommand<TPassConfig,THandler>`).
2. Implement matching `ICommandHandler`.
3. Optionally add `IPassConfig` if it needs a different render target/state profile.
4. Emit command from view `CollectContent` or extension methods.

### Add a custom shader-backed effect

1. Add `.slang` file under content path.
2. Load via `IGraphicsModule.Get().MakeGraphics(...)` or `MakeCompute(...)`.
3. Use bind context writes/push constants/draw-dispatch in handler/pass.

---

## 13) Current Gaps / Caveats (Observed in Code)

- `VulkanGraphicsModule.AddRenderer/RemoveRenderer` are `NotImplementedException`.
- Some async image creation paths are not implemented:
  - `CreateVulkanTextureArray(...)` (data upload variant)
  - `CreateVulkanCubemap(...)` (data upload variant)
- `GraphConfig.CreateTextureArray(...)` currently throws `NotImplementedException` before return.
- `SlangSessionBuilder.LoadFile(...)` is intentionally not implemented (manager currently handles include expansion itself).
- `WindowRenderer` currently uses `FramesInFlight = 1`.

---

## 14) Read-First File Map

If onboarding to rendering, read in this order:

1. `Rin.Framework/Application.cs`
2. `Rin.Framework.Graphics.Vulkan/VulkanGraphicsModule.cs`
3. `Rin.Framework.Graphics.Vulkan/WindowRenderer.cs`
4. `Rin.Framework.Graphics.Vulkan/Graph/GraphBuilder.cs`
5. `Rin.Framework.Graphics.Vulkan/Graph/GraphConfig.cs`
6. `Rin.Framework.Graphics.Vulkan/Graph/CompiledGraph.cs`
7. `Rin.Framework/Views/ViewsModule.cs`
8. `Rin.Framework/Views/Window/WindowSurface.cs`
9. `Rin.Framework/Views/Graphics/DefaultCollectedSurfaceData.cs`
10. `Rin.Framework/Views/Graphics/Passes/*`
11. `Rin.Framework.Graphics.Vulkan/VulkanExecutionContext.cs`
12. `Rin.Framework.Graphics.Vulkan/Shaders/Slang/SlangShaderManager.cs`
13. `Rin.Framework.Graphics.Vulkan/Images/VulkanBindlessImageFactory.cs`
14. `Rin.Framework.Graphics.Vulkan/Graph/ResourcePool.cs`

---

## 15) End-to-End Sequence (Condensed)

1. Views and other systems gather visual state during update.
2. On collect (main thread), each window surface emits `WindowSurfaceCollectedData` to graph collector.
3. On render thread, `WindowRenderer` builds and compiles frame graph.
4. Graph allocates/aliases resources, inserts barriers, and executes passes.
5. Views passes render into offscreen main image + stencil clips.
6. Main image is copied to swapchain image.
7. Swapchain image transitions to present and is presented.


