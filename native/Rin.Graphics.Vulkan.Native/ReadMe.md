# Rin.Graphics.Vulkan.Native

Native layer for the Vulkan backend. Builds `Rin.Graphics.Vulkan.Native` as a shared library and packs it as `TareHimself.Rin.Graphics.Vulkan.Native`.

## Where it fits

- `Engine/Rin.Graphics.Vulkan` references the `TareHimself.Rin.Graphics.Vulkan.Native` package.
- No fake exists for it. CI uses the stub package.

## Start here

- `CMakeLists.txt`: links `rwin`, `Vulkan::Vulkan`, `vk-bootstrap` and `vulkan-memory-allocator`.
- `silopkg.py`: the silo recipe. Depends on `rwin@1.0.0` and `vk-bootstrap@1.3.296` (both static, so the package holds one DLL), `vulkan-headers@1.3.296`, `vulkan-loader@1.3.296` and `vulkan-memory-allocator@3.1.0`, whose recipe is in `../recipes`. `Vulkan::Vulkan` is an alias for the loader silo builds.
- `src/`: `graphics.*`, `platform.*`, `flags.hpp`, `macro.hpp`.
- `project.csproj`: shell project used only by `dotnet pack`.

## Device selection

`createVulkanInstance` (in `src/graphics.cpp`) requires Vulkan 1.3 with dynamic rendering, synchronization2, buffer device address, descriptor indexing and scalar block layout. It prefers a GPU that also has the indirect-draw features the engine uses (`multiDrawIndirect`, `drawIndirectCount` and `drawIndirectFirstInstance`), enables them, and reports `supportsIndirectRendering`. If no GPU has all three, it still creates the device without them and the flag is false, and `Rin.World` falls back to direct draws.

## Build and pack

From the repo root:

```
uv run python scripts/pack_native.py Rin.Graphics.Vulkan.Native
```

For a build to work on, `uv run silo sync native/Rin.Graphics.Vulkan.Native` builds the dependencies and writes the `Rin-Debug` and `Rin-Release` CMake presets, which an IDE can open or `uv run silo run -C native/Rin.Graphics.Vulkan.Native -- cmake --preset Rin-Debug` can configure from this folder. See [../ReadMe.md](../ReadMe.md) for the packing flow.

## Gotchas

- The build does not use the Vulkan SDK. It links the loader silo builds, and the package does not ship it: at run time the machine's own `vulkan-1.dll` is used.

The clipboard is text only: `platformHasClipboardText`, `platformGetClipboardText` (UTF-16, valid until the next clipboard call, null when the clipboard holds no text), `platformSetClipboardText` and `platformClearClipboard` wrap rwin's clipboard calls.

Drag and drop goes through `platformWindowSetDropCallbacks`, which wraps managed function pointers into rwin's per-window `DropCallbacks`. Each callback gets a `PlatformDropInfo` (window local position, allowed `rwin::DropEffect` flags, modifiers) and a `PlatformDropData` with the dragged text as UTF-16 and the dragged files as UTF-8 paths, both only valid during the callback, and returns the effect to apply. `platformWindowClearDropCallbacks` removes them.
