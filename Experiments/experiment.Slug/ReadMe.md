# experiment.Slug

SLUG GPU vector rendering through a `CanvasView`: two lines of text from a system font (outlines extracted with SixLabors.Fonts) and a hand-authored star, all rasterized by the same SLUG shader. Opens a 900x600 window.

```
dotnet run --project Experiments/experiment.Slug/experiment.Slug.csproj
```

`HOW_SLUG_WORKS.md` in this folder explains the technique.

References: Rin.Core, Rin.Graphics.Vulkan, Rin.Audio.Miniaudio, Rin.Shade, Rin.SourceGenerators, Examples.Common. Packages: SixLabors.Fonts.
