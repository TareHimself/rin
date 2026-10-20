# Rin.GLTF

Imports glTF and glb files into Rin meshes and animation clips: `GltfMeshImporter` loads static and skinned meshes and `GltfAnimationImporter` loads animation clips, both through `SharpGLTF.Core`.

```
dotnet build Engine/Rin.GLTF/Rin.GLTF.csproj
dotnet test Engine/Rin.GLTF.Tests/Rin.GLTF.Tests.csproj -p:RinShadeSkipCompile=true
```

The tests need the native fake published first, see [Rin.GLTF.Tests](../Rin.GLTF.Tests/ReadMe.md).

References: Rin.World (and Rin.Core through it), SharpGLTF.Core. Used by Rin.GLTF.Tests and Examples.
