# Rin.GLTF.Tests

NUnit tests for [Rin.GLTF](../Rin.GLTF/ReadMe.md), loading the `fox.glb` and `cube.glb` models linked from `Examples/Examples/assets/models/`, and the `Assets/blender_level/` folder (`level.glb` and its environment map), a level exported by the [Blender add-on](../../Tools/Blender/rin/ReadMe.md) through `Assets/make_blender_level.py`. Its components come from `Assets/editor.schema.json`, which a test keeps equal to the schema of the test assembly's components.

```
dotnet test Engine/Rin.GLTF.Tests/Rin.GLTF.Tests.csproj -p:RinShadeSkipCompile=true
```

Nothing in the import path touches native code, so the tests need no native package beyond the stub that satisfies restore.

References: Rin.GLTF, Rin.Graphics.Null.
