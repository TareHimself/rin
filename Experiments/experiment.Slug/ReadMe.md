# experiment.Slug

Demonstrates SLUG GPU vector rendering through a `CanvasView`. Glyph outlines (extracted with SixLabors.Fonts) and a hand-authored star path are rasterized by the same SLUG shader. Opens a 900x600 window with sample text and the star.

```
dotnet run --project Experiments/experiment.Slug/experiment.Slug.csproj
```

`HOW_SLUG_WORKS.md` in this folder explains the technique. The CPU side is in `Rendering/`, the shader in `Shaders/`.

References: Rin.Core, Rin.Graphics.Vulkan, Rin.Audio.Miniaudio, Rin.Shade, Rin.SourceGenerators, Examples.Common.
