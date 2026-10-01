# experiments.Docking

Docking system demo: one window with an initial docked layout. Tabs can be dragged to redock, splitters dragged to resize and tabs closed with the close button. The model (`DockTree`, `DockNode`, `DockPanel`) is in `Model/`, the views in `Views/`. `SceneTest` references this project for its dock layout.

```
dotnet run --project Experiments/experiments.Docking/experiments.Docking.csproj
dotnet run --project Experiments/experiments.Docking/experiments.Docking.csproj -- test
```

`test` runs `DockTreeSmokeTest` without opening a window and exits with its result code. In the window, R resets the layout.

References: Rin.Core, Rin.Graphics.Vulkan, Rin.Audio.Miniaudio, Examples.Common.
