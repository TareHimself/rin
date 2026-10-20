# Rin.GLTF.Tests

NUnit tests for [Rin.GLTF](../Rin.GLTF/ReadMe.md), loading the `fox.glb` and `cube.glb` models linked from `Examples/Examples/assets/models/` and copied to `Assets/` in the output.

Publish the native fake first, then test (the order CI uses):

```
uv run python scripts/publish_native_fakes.py win-x64
dotnet test Engine/Rin.GLTF.Tests/Rin.GLTF.Tests.csproj -p:RinShadeSkipCompile=true
```

- A setup fixture redirects Rin.Core's `Rin.Native` import to the fake published under `native/Fakes/Rin.Native.Fake`, found by searching upward for `rin.sln`. Without it the tests fail with "Rin.Native.Fake.dll not published".

References: Rin.GLTF, Rin.Graphics.Null.
