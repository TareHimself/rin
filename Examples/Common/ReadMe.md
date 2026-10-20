# Examples.Common

Shared library for [Examples](../Examples/ReadMe.md) and the experiment apps under `Experiments/`, not runnable on its own. It holds the base `ExampleApplication` (Vulkan graphics, views and Miniaudio audio modules), the source-tagged `Log`, a `TextureCache` and a few shared views.

```
dotnet build Examples/Common/Examples.Common.csproj
```

References: Rin.Core, Rin.Graphics.Vulkan, Rin.Audio.Miniaudio.
