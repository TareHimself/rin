# Rin.GLTF.Tests

NUnit tests for [Rin.GLTF](../Rin.GLTF/ReadMe.md), loading the `fox.glb` and `cube.glb` models linked from `Examples/Examples/assets/models/`, and `Assets/blender_level.glb`, a level exported by the [Blender add-on](../../Tools/rin_blender/ReadMe.md) through `Assets/make_blender_level.py`.

```
dotnet test Engine/Rin.GLTF.Tests/Rin.GLTF.Tests.csproj -p:RinShadeSkipCompile=true
```

Nothing in the import path touches native code, so the tests need no native package beyond the stub that satisfies restore.

References: Rin.GLTF, Rin.Graphics.Null.
