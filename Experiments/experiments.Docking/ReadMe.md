# experiments.Docking

Docking system demo: one window with an initial docked layout. Drag a tab to redock it, drag a splitter to resize, click the x on a tab to close it, press R to reset the layout. The `scene-test` example in [Examples](../../Examples/Examples/ReadMe.md) uses this project for its dock layout.

```
dotnet run --project Experiments/experiments.Docking/experiments.Docking.csproj
dotnet run --project Experiments/experiments.Docking/experiments.Docking.csproj -- test
```

`test` runs the `DockTree` smoke test without opening a window and exits with its result code.

References: Rin.Core, Rin.Graphics.Vulkan, Rin.Audio.Miniaudio, Examples.Common.
