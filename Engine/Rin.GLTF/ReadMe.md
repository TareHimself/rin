# Rin.GLTF

Imports glTF/glb files into Rin meshes and animations, using `SharpGLTF.Core`.

## Where it fits

- References: Rin.World (and through it Rin.Core) and the `SharpGLTF.Core` package.
- Referenced by: Rin.GLTF.Tests and applications under `Examples/`.

## Start here

- `GltfMeshImporter.cs`: static class that loads meshes.
- `GltfAnimationImporter.cs`: static class that loads animations.

## Build and test

```
dotnet build Engine/Rin.GLTF/Rin.GLTF.csproj
dotnet test Engine/Rin.GLTF.Tests/Rin.GLTF.Tests.csproj -p:RinShadeSkipCompile=true
```

The tests need the native fakes published first, see [Rin.GLTF.Tests](../Rin.GLTF.Tests/ReadMe.md).
