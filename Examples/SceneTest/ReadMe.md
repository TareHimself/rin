# SceneTest

3D world with Bepu physics shown in a dockable layout (`DockSpaceView` from `experiments.Docking`) with a perspective camera and a top camera. Uses glTF assets from `assets/`.

```
dotnet run --project Examples/SceneTest/SceneTest.csproj
```

Options and controls (from the code):
- `--log-file <path>` redirects stdout and stderr to a file.
- W, A, S, D move the camera in the viewport. P drops a grid of 2000 boxes.

References: Rin.World, Rin.Core, Rin.GLTF, Examples.Common, experiments.Docking.
