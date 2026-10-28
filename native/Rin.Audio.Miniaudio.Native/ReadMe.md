# Rin.Audio.Miniaudio.Native

Native wrapper over miniaudio for `Engine/Rin.Audio.Miniaudio`. Built as the shared library `Rin.Audio.Miniaudio.Native` and packed as `TareHimself.Rin.Audio.Miniaudio.Native`.

```
uv run python scripts/pack_native.py Rin.Audio.Miniaudio.Native
```

See [../ReadMe.md](../ReadMe.md) for the packing flow and the `silo sync` and `silo run` commands for a build to work on.

| Dependency | Version | Notes |
| --- | --- | --- |
| miniaudio | 0.11.22 | Linked privately. The package holds one DLL. |

No fake exists for it. CI uses the stub package.

- `audioInit`, every factory (`audioMake*`, `audioSampleMakeActive`, `audioMixerCreate`, `audioMixerPlay`, `audioMixerCreatePushStream`, `audioCreateScene`, `audioScenePlay*`) and `audioMixerAddEffect` return an `AudioResult` from `src/result.hpp` and hand the new handle back through an out parameter. `MiniaudioEngine.AudioResult` in `Engine/Rin.Audio.Miniaudio/MiniaudioEngine.cs` mirrors the enum. The `audioActive*` play, pause, volume and seek exports still return 1 or 0 for "applied".
