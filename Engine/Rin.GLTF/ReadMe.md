# Rin.GLTF

Imports glTF and glb files into Rin: `GltfMeshImporter` loads static and skinned meshes, `GltfAnimationImporter` loads animation clips, and `GltfSceneImporter` spawns a whole scene into a world, all through `SharpGLTF.Core`. glTF is right-handed and Rin left-handed, so the importers mirror X: a model facing glTF's +Z faces Rin's forward, and left and right match what the authoring tool showed.

`GltfSceneImporter.Load(path, renderFeature, resolveMaterial)` spawns one entity per node with its static mesh (one upload per glTF mesh), `KHR_lights_punctual` lights and perspective cameras. The caller turns each glTF material into an `IMeshMaterial`. Skinned meshes and orthographic cameras are skipped and reported in `Warnings`. These extras override the glTF values:

| Extra | On | Meaning |
| --- | --- | --- |
| `rin_lumens` | light node | Point light brightness in lumens, instead of glTF's candela times 4π |
| `rin_lux` | light node | Directional light brightness in lux |
| `rin_start_camera` | scene | Name of the camera node returned as `StartCamera`, instead of the first camera |

```
dotnet build Engine/Rin.GLTF/Rin.GLTF.csproj
dotnet test Engine/Rin.GLTF.Tests/Rin.GLTF.Tests.csproj -p:RinShadeSkipCompile=true
```

The tests need the native fake published first, see [Rin.GLTF.Tests](../Rin.GLTF.Tests/ReadMe.md).

References: Rin.World (and Rin.Core through it), SharpGLTF.Core. Used by Rin.GLTF.Tests and Examples.
