# experiment.StencilAndCover

GPU stencil-and-cover path filling through a `CanvasView`: a concave star to check anti-aliasing at sharp corners, and a square ring whose hole comes from an oppositely wound inner contour under nonzero winding. Opens a 900x600 window.

```
dotnet run --project Experiments/experiment.StencilAndCover/experiment.StencilAndCover.csproj
```

References: Rin.Core, Rin.Graphics.Vulkan, Rin.Audio.Miniaudio, Rin.Shade, Rin.SourceGenerators, Examples.Common.
