# Experiments

Prototype projects for rendering and UI ideas. They build on `Examples.Common` and use the same run command as the examples:

```
dotnet run --project Experiments/<Project>/<Project>.csproj
```

| Project | What it does | Engine and shared references |
| --- | --- | --- |
| `experiment.FontIcon` | Draws a grid of Font Awesome 6 Free solid icons through the normal font manager and MTSDF text renderer | Rin.Core, Rin.Graphics.Vulkan, Rin.Audio.Miniaudio, Examples.Common |
| `experiment.Slug` | SLUG GPU vector rendering of glyph outlines and a hand-built path inside a `CanvasView` | Rin.Core, Rin.Graphics.Vulkan, Rin.Audio.Miniaudio, Rin.Shade, Rin.SourceGenerators, Examples.Common |
| `experiment.StencilAndCover` | GPU stencil-and-cover fill of a concave star and a two-contour ring | Rin.Core, Rin.Graphics.Vulkan, Rin.Audio.Miniaudio, Rin.Shade, Rin.SourceGenerators, Examples.Common |
| `experiments.Docking` | Docking system: tab groups, splits and drag-to-redock panels | Rin.Core, Rin.Graphics.Vulkan, Rin.Audio.Miniaudio, Examples.Common |

`misc.StrokeExpansion` and `misc.VectorRendering` currently contain only build output folders, no source.
