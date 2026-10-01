# experiment.FontIcon

Smoke test for `FontIconView`: loads Font Awesome 6 Free's solid icon font (icon glyphs at private-use codepoints) through the ordinary font manager and draws a grid of 15 icons through the MTSDF renderer, with white, orange and cyan tint options. No icon-specific rendering code is involved.

```
dotnet run --project Experiments/experiment.FontIcon/experiment.FontIcon.csproj
```

References: Rin.Core, Rin.Graphics.Vulkan, Rin.Audio.Miniaudio, Examples.Common.
