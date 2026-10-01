# AssetViewer

Loads the skinned `fox.glb` model (copied from `SceneTest/assets/models`), places the camera to frame its bounds and loops the "Run" animation clip. Model path, clip name and camera framing are hardcoded in `AssetViewerApplication.cs`. There are no input controls.

```
dotnet run --project Examples/AssetViewer/AssetViewer.csproj
```

References: Rin.World, Rin.Core, Rin.GLTF, Examples.Common.
