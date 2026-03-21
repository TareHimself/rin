# AGENTS.md

## Repo at a glance
- `Rin.Framework` is the core runtime: app loop, DI/provider, sources, views, graphics/audio abstractions.
- Typical runnable apps use `Application` (`Rin.Framework/Application.cs`) and wire modules by overriding `CreateGraphicsModule`, `CreateViewsModule`, `CreateAudioModule` (see `rin.Examples/Common/ExampleApplication.cs`).
- Concrete backends are split into projects: `Rin.Framework.Graphics.Vulkan` (`VulkanGraphicsModule`) and `Rin.Framework.Audio.Bass` (`BassAudioModule`).
- `Rin.Framework.NativeLibs*` projects build/package C++ native binaries consumed by framework/backend packages.
- `Rin.Engine.World`, `Rin.Editor`, `misc.*` contain partially migrated/legacy code paths and are not a reliable baseline for new features.

## Execution model (important)
- `Application.Run()` is dual-threaded: main update thread + render thread synchronized by `_mainUpdateEvent` / `_renderFinishedEvent` (`Rin.Framework/Application.cs`).
- Update pipeline order: `OnPreUpdate` -> `MainDispatcher.DispatchPending()` -> `OnUpdate(delta)` -> `OnPostUpdate` -> `OnCollect`.
- Render thread pipeline: `RenderDispatcher.DispatchPending()` -> `OnPreRender` -> `OnRender` -> `OnPostRender`.
- Modules register by subscribing/unsubscribing to those events in `Start`/`Stop` (example: `VulkanGraphicsModule.Start/Stop`, `ViewsModule.Start/Stop`).

## Project conventions to follow
- Prefer provider singletons over globals: `IApplication.Get()`, `IGraphicsModule.Get()`, `IViewsModule.Get()`, `IAudioModule.Get()`.
- Resource paths go through `SFramework.Sources`; default sources include `FileSystemSource` (`fs/...`) and embedded `Framework/...` content (`Rin.Framework/SFramework.cs`).
- Embedded assets are loaded via `AssemblyContentResource.New<T>(alias)`; example font load: `ViewsModule` reads `Framework/Fonts/NotoSans-Regular.ttf`.
- Runtime targets are `net9.0` across projects; `global.json` pins SDK `8.0.0` with `rollForward: latestMajor`.
- Several projects still reference removed APIs like `SFramework.Get()` / `SEngine`; do not copy these patterns into new code.

## Build, test, and local packaging workflow
- Baseline test project: `Rin.Framework.Tests` (NUnit). `dotnet test Rin.Framework.Tests/Rin.Framework.Tests.csproj` currently passes.
- Full solution build (`dotnet build rin.sln`) currently fails in legacy/migration projects (`Rin.Engine.World`, `Rin.Editor`, `misc.VectorRendering`, `misc.StrokeExpansion`), while core framework/examples mostly build.
- For day-to-day changes, build targeted projects instead of whole solution when touching framework/runtime paths.
- `Taskfile.yml` root task `nuget-local` packs/pushes local feed variants of `Rin.Framework`, audio, and vulkan packages using `DepMode=local_feed`.
- Native lib csprojs execute CMake/Conan (`task conand`, `cmake --preset Rin-$(Configuration)` in `*/native`); ensure Vulkan SDK + CMake + Conan are installed.

## Integration points and hotspots
- Shader loading commonly uses filesystem source paths, e.g. `fs/{absolute-path}` in `rin.Examples/ViewsTest/CustomShaderCommandHandler.cs`.
- Window lifecycle originates in graphics module events (`OnWindowCreated`, `OnWindowRendererCreated`) and views surfaces are attached in `ViewsModule`.
- UI scene composition pattern: subscribe on startup, create window, then add root view in surface callback (see `NodeGraphTest/MainApplication.cs`, `rin.Examples/ViewsTest/ViewsTestApplication.cs`).
- Audio backend is partially implemented (`BassAudioModule` has `NotImplementedException` for sample/scene paths; see `Rin.Framework/Audio/IAudioScene.cs`).

## Safe contribution strategy for agents
- Start from `NodeGraphTest` or `rin.Examples/*` for executable patterns; avoid using `misc.*` and `Rin.Engine.World` as source-of-truth.
- When changing threading or module lifecycle code, verify event unsubscription/disposal symmetry in `Start`/`Stop`.
- If you add assets, wire them through `Content/**` + `AssemblyContentResource` or explicit `fs/...` source paths.
- Validate with targeted `dotnet build` + `dotnet test` on affected projects before attempting full solution build.

