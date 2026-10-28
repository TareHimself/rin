# Rin.GLTF.Tests

NUnit tests for [Rin.GLTF](../Rin.GLTF/ReadMe.md), loading the `fox.glb` and `cube.glb` models linked from `Examples/Examples/assets/models/` and copied to `Assets/` in the output.

```
dotnet test Engine/Rin.GLTF.Tests/Rin.GLTF.Tests.csproj -p:RinShadeSkipCompile=true
```

Nothing in the import path touches native code, so the tests need no native package beyond the stub that satisfies restore.

References: Rin.GLTF, Rin.Graphics.Null.
