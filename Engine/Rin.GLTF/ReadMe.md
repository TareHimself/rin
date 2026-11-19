# Rin.GLTF

Imports glTF and glb files into Rin meshes and animation clips: `GltfMeshImporter` loads static and skinned meshes and `GltfAnimationImporter` loads animation clips, both through `SharpGLTF.Core`. glTF is right-handed and Rin left-handed, so the importers mirror X: a model facing glTF's +Z faces Rin's forward, and left and right match what the authoring tool showed.

```
dotnet build Engine/Rin.GLTF/Rin.GLTF.csproj
dotnet test Engine/Rin.GLTF.Tests/Rin.GLTF.Tests.csproj -p:RinShadeSkipCompile=true
```

The tests need the native fake published first, see [Rin.GLTF.Tests](../Rin.GLTF.Tests/ReadMe.md).

References: Rin.World (and Rin.Core through it), SharpGLTF.Core. Used by Rin.GLTF.Tests and Examples.
