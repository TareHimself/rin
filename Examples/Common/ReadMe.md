# Examples.Common

Shared library for [Examples](../Examples/ReadMe.md) and the experiment apps, not runnable on its own.

- `ExampleApplication`: base `Application` that creates the Vulkan graphics module, `ViewsModule` and Miniaudio audio module, and disposes a `TextureCache`.
- `Log`: the examples' logger. `Log.For<T>()` or `Log.For("name")` gives a source-tagged `Info`, `Warn` and `Error`; all of them write through one engine `DefaultLogger` to the console as `HH:mm:ss.fff level Source: message`, so `--log-file` captures them too.
- `Views/FpsView`, `Views/AsyncFileImageView`, `Views/AsyncWebImageView`.

References: Rin.Core, Rin.Graphics.Vulkan, Rin.Audio.Miniaudio.
