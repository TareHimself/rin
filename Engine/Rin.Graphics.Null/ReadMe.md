# Rin.Graphics.Null

A graphics backend that does nothing, for headless runs and tests. Package id `TareHimself.Rin.Graphics.Null`.

## Where it fits

- References: Rin.Core only.
- Referenced by: Rin.GLTF.Tests, and several projects under `Examples/` and `Experiments/` (including `Examples/HeadlessTest`).

## Start here

- `NullGraphicsModule.cs`: the `IGraphicsModule` implementation (sealed).
- `NullDevice.cs`, `NullWindow.cs`, `NullGraphicsShader.cs`, `NullComputeShader.cs`: no-op device, window and shaders.

## Build

```
dotnet build Engine/Rin.Graphics.Null/Rin.Graphics.Null.csproj
```

It has no tests of its own. It is exercised through Rin.GLTF.Tests.
