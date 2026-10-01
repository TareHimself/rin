# Rin.Graphics.Vulkan

Vulkan backend for the `IGraphicsModule` interface from Rin.Core. Package id `TareHimself.Rin.Graphics.Vulkan`.

## Where it fits

- References (default `local_file` mode): Rin.Core and `Slang/Rin.Slang`. In `online` mode it uses the matching NuGet packages instead.
- Also uses `TareHimself.Rin.Graphics.Vulkan.Native` (from the local `.feed/`) and `TerraFX.Interop.Vulkan`.
- Used by applications under `Examples/` and `Experiments/`. Nothing else in `Engine/` references it.

## Start here

- `VulkanGraphicsModule.cs` and `VulkanGraphicsModule.Resources.cs`: the module entry point and resource creation.
- `VulkanDevice.cs`, `WindowRenderer.cs`, `Frame.cs`: device, per-window rendering and frame state.
- `Graph/` (`GraphBuilder.cs`, `CompiledGraph.cs`, resource descriptors and passes): the render graph.
- `Images/`, `Descriptors/`, `Shaders/`, `Windows/`: textures, descriptor sets, shader binding, window handling.
- `BufferWriteLanes.cs`, `TextureWriteLanes.cs`, `WriteRecorder.cs`: pending buffer and texture writes.

## Build

```
dotnet build Engine/Rin.Graphics.Vulkan/Rin.Graphics.Vulkan.csproj
```

The native packages must be in `.feed/` first (`task pack-all`, see the root `ReadMe.md`). There is no test project for this backend.

## Notes

- Unsafe code is enabled and the project is marked AOT compatible.
