# Rin.GLTF.Tests

NUnit tests for [Rin.GLTF](../Rin.GLTF/ReadMe.md).

## Where it fits

- References: Rin.GLTF and Rin.Graphics.Null.
- The test models `fox.glb` and `cube.glb` are linked from `Examples/Examples/assets/models/` and copied to `Assets/` in the output.

## Start here

- `GltfMeshImporterTests.cs`, `GltfAnimationImporterTests.cs`: the tests.
- `NativeFakeSetup.cs`: a `[SetUpFixture]` that redirects the `Rin.Native` DllImport to a fake library.
- `NativeFakeLocator.cs`: finds the published fake under `native/Fakes/Rin.Native.Fake`, searching upward for `rin.sln`.

## Run

Publish the native fake first, then test (this is the order CI uses):

```
uv run python scripts/publish_native_fakes.py win-x64
dotnet test Engine/Rin.GLTF.Tests/Rin.GLTF.Tests.csproj -p:RinShadeSkipCompile=true
```

## Gotcha

If the fake has not been published, the setup throws "Rin.Native.Fake.dll not published".
