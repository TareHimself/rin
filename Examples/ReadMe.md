# Examples

Sample and demo apps for the engine. Most open a Vulkan window and use `Examples.Common` (`ExampleApplication`), which wires up the Vulkan graphics module, the views module and the Miniaudio audio module. Run any project from the repo root (see the root `ReadMe.md` for the native-package setup first):

```
dotnet run --project Examples/<Project>/<Project>.csproj
```

| Project | What it does | Engine and shared references |
| --- | --- | --- |
| `AssetViewer` | Loads `fox.glb` (from `SceneTest/assets`), frames it and plays the "Run" clip | Rin.World, Rin.Core, Rin.GLTF, Examples.Common |
| `AudioPlayer` | Image switcher plus audio player views, with YouTube and Spotify lookup packages | Rin.Core, Examples.Common |
| `ChatApp` | Chat UI views. Currently a scratch `Program.cs`, the module code is commented out | Rin.Core |
| `Common` | Shared library: `ExampleApplication`, `TextureCache`, `FpsView`, async image views | Rin.Core, Rin.Graphics.Vulkan, Rin.Audio.Miniaudio |
| `HeadlessTest` | Runs a physics world with null graphics and audio, prints a falling sphere's height | Rin.Core, Rin.World, Rin.Graphics.Null, Rin.Audio.Null |
| `NodeGraphTest` | Node graph editor views with typed pins | Rin.Core, Rin.Graphics.Vulkan, Examples.Common |
| `RLTest` | Scratch program that composites a host image and saves a PNG | Rin.Core |
| `SceneTest` | 3D world with Bepu physics in a dockable layout with perspective and top cameras | Rin.World, Rin.Core, Rin.GLTF, Examples.Common, experiments.Docking |
| `Sponza` | Loads the Sponza glb with a free-fly camera and a custom Shade material | Rin.World, Rin.Shade, Examples.Common |
| `UiGallery` | Scrollable gallery of quad modes, blur and clipping | Examples.Common |
| `ViewsTest` | Views and audio effects test bench | Rin.Shade, Rin.SourceGenerators, Examples.Common |

`ChatApp` and `RLTest` are not polished demos (see their ReadMe files).
