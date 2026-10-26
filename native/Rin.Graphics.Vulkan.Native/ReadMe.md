# Rin.Graphics.Vulkan.Native

Native layer for `Engine/Rin.Graphics.Vulkan`: Vulkan setup, windowing and the memory allocator. Built as the shared library `Rin.Graphics.Vulkan.Native` and packed as `TareHimself.Rin.Graphics.Vulkan.Native`.

```
uv run python scripts/pack_native.py Rin.Graphics.Vulkan.Native
```

See [../ReadMe.md](../ReadMe.md) for the packing flow and the `silo sync` and `silo run` commands for a build to work on.

| Dependency | Version | Notes |
| --- | --- | --- |
| rwin | 1.0.0 | Windowing. Built from the commit in `../silo.config.toml`. |
| vk-bootstrap | 1.3.296 | Static. |
| vulkan-headers | 1.3.296 | |
| vulkan-loader | 1.3.296 | Aliased as `Vulkan::Vulkan` in CMake. |
| vulkan-memory-allocator | 3.1.0 | One header, recipe in `../recipes`. |

No fake exists for it. CI uses the stub package.

- The build does not use the Vulkan SDK, and the package ships only `Rin.Graphics.Vulkan.Native.dll`: at run time it loads the machine's own `vulkan-1.dll`.
- An export that can fail returns a `GraphicsResult` from `src/result.hpp` and writes its value through an out parameter. The body runs inside `guarded`, which turns a `ResultError`, a `std::bad_alloc` or any other exception into a code, so nothing unwinds into the managed caller. `Native.GraphicsResult` in `Engine/Rin.Graphics.Vulkan/Native.cs` mirrors the enum, so a new value is added to both.
