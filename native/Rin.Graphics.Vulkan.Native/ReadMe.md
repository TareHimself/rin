# Rin.Graphics.Vulkan.Native

Native layer for the Vulkan backend. Builds `Rin.Graphics.Vulkan.Native` as a shared library and packs it as `TareHimself.Rin.Graphics.Vulkan.Native`.

## Where it fits

- `Engine/Rin.Graphics.Vulkan` references the `TareHimself.Rin.Graphics.Vulkan.Native` package.
- No fake exists for it. CI uses the stub package.

## Start here

- `CMakeLists.txt`: links `rwin`, `Vulkan::Vulkan`, `vk-bootstrap` and `GPUOpen::VulkanMemoryAllocator` (fetched with `FetchContent`).
- `conanfile.py`: requires `vk-bootstrap/1.3.296`, `msdfgen/1.12` and `rwin/1.0.1` (with the `compat` option on macOS).
- `src/`: `graphics.*`, `platform.*`, `flags.hpp`, `macro.hpp`.
- `project.csproj`: shell project used only by `dotnet pack`.

## Device selection

`createVulkanInstance` (in `src/graphics.cpp`) requires Vulkan 1.3 with dynamic rendering, synchronization2, buffer device address, descriptor indexing and scalar block layout. It prefers a GPU that also has the indirect-draw features the engine uses (`multiDrawIndirect`, `drawIndirectCount` and `drawIndirectFirstInstance`), enables them, and reports `supportsIndirectRendering`. If no GPU has all three, it still creates the device without them and the flag is false, and `Rin.World` falls back to direct draws.

## Build and pack

From the repo root:

```
uv run task pack-graphics-vulkan-native
```

Or from this folder, `uv run task build` (Release) or `uv run task buildd` (Debug). See [../ReadMe.md](../ReadMe.md) for the packing flow.

## Gotchas

- Needs the Vulkan SDK installed (`find_package(Vulkan REQUIRED)`).
