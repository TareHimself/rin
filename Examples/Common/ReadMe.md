# Examples.Common

Shared library for [Examples](../Examples/ReadMe.md) and the experiment apps, not runnable on its own.

- `ExampleApplication`: base `Application` that creates the Vulkan graphics module, `ViewsModule` and Miniaudio audio module, and disposes a `TextureCache`.
- `Views/FpsView`, `Views/AsyncFileImageView`, `Views/AsyncWebImageView`.

References: Rin.Core, Rin.Graphics.Vulkan, Rin.Audio.Miniaudio.
