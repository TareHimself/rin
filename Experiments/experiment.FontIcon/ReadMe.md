# experiment.FontIcon

Smoke test for `FontIconView`: loads the Font Awesome 6 Free solid font through the ordinary font manager and draws a grid of 15 icons in white, orange and cyan. Nothing icon-specific is involved, the icons go through the same MTSDF path as text.

```
dotnet run --project Experiments/experiment.FontIcon/experiment.FontIcon.csproj
```

References: Rin.Core, Rin.Graphics.Vulkan, Rin.Audio.Miniaudio, Examples.Common.
