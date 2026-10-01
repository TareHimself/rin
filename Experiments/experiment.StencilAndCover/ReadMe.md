# experiment.StencilAndCover

Demonstrates GPU stencil-and-cover path filling through a `CanvasView`: a concave five-point star (checks anti-aliasing at sharp corners) and a two-contour ring (checks hole cutting by opposite contour direction under nonzero winding). Opens a 900x600 window.

```
dotnet run --project Experiments/experiment.StencilAndCover/experiment.StencilAndCover.csproj
```

CPU side (path flattening, fan triangulation, commands) is in `Rendering/`, shaders in `Shaders/`.

References: Rin.Core, Rin.Graphics.Vulkan, Rin.Audio.Miniaudio, Rin.Shade, Rin.SourceGenerators, Examples.Common.
