# Rin.Native

Core native helpers for `Engine/Rin.Core`: memory, platform, SDF generation and webm video decoding. Built as the shared library `Rin.Native` and packed as `TareHimself.Rin.Native`.

```
uv run python scripts/pack_native.py Rin.Native
```

See [../ReadMe.md](../ReadMe.md) for the packing flow and the `silo sync` and `silo run` commands for a build to work on.

| Dependency | Version | Notes |
| --- | --- | --- |
| webmdx | 1.0.0 | Static. Built from the commit in `../silo.config.toml`. |
| msdfgen | 1.12 | Static, core library only (no PNG, SVG or command line parts). |

Both dependencies are static, so the package holds `Rin.Native.dll` alone. `Engine/Rin.GLTF.Tests` uses the NativeAOT fake instead, see [../Fakes/ReadMe.md](../Fakes/ReadMe.md).

- `platformInit`, the file and path dialogs, `sdfContextNew`, `sdfContextGenerateMTSDF`, `videoContextCreate` and `videoSourceCreate` return a `CoreResult` from `src/result.hpp`, mirrored by `Native.CoreResult` in `Engine/Rin.Core/Native.cs`. The video decode and seek exports keep their own `VideoDemuxResult`, which describes the stream rather than the call.
