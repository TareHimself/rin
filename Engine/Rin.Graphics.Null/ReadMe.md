# Rin.Graphics.Null

A graphics backend that does nothing: `NullGraphicsModule` implements `IGraphicsModule` with a no-op device, window and shaders, for headless runs and tests. Package id `TareHimself.Rin.Graphics.Null`.

```
dotnet build Engine/Rin.Graphics.Null/Rin.Graphics.Null.csproj
```

- A headless run still paces like a windowed one: `SetMaxFramerate` is honoured through a `FrameLimiter` even though nothing is drawn.

It has no tests of its own and is exercised through Rin.GLTF.Tests.

References: Rin.Core. Used by Rin.GLTF.Tests and Examples (the `headless` example).
