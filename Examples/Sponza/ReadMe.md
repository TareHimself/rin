# Sponza

Loads `sponza.glb` on a background task, then shows it through a viewport with a point-light camera, a custom Shade-based mesh material (`SponzaMeshMaterial`) and an FPS counter in the top right.

```
dotnet run --project Examples/Sponza/Sponza.csproj
```

Controls (from `TestViewport.cs`): W, A, S, D move the camera while the viewport is focused, mouse movement turns it.

References: Rin.World, Rin.Shade (plus its source generator), Examples.Common.
