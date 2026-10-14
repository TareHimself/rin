# Rin.Audio.Miniaudio

Audio backend built on miniaudio, implementing `IAudioModule` from Rin.Core. Package id `TareHimself.Rin.Audio.Miniaudio`.

## Where it fits

- References: Rin.Core (project reference in `local_file` mode, NuGet in `online` mode) and the `TareHimself.Rin.Audio.Miniaudio.Native` package from the local `.feed/`.
- Nothing else in `Engine/` references it. Applications choose it as their audio module.

## Start here

- `MiniaudioAudioModule.cs`: the module entry point.
- `MiniaudioAudioSample.cs`, `MiniaudioActiveAudio.cs`, `MiniaudioGroup.cs`, `MiniaudioDirectionalBus.cs`, `MiniaudioPushStream.cs`: samples, playback, groups and streams. `HasEnded` reads miniaudio's at-end flag, and the native side reports the sound's length as its position once that flag is set, since miniaudio wraps the cursor back to 0 there.
- `MiniaudioEffectController.cs`, `NativeEffectManager.cs`: audio effects. `MiniaudioAudioGroup.RemoveEffect` and disposing a controller both end in the group, which drops its dictionary entry first and then removes the effect natively, and disposing a group removes its remaining effects before the mixer. Parameter updates go through `ParameterSlots<T>.Write`.
- `Native.cs`: P/Invoke declarations for the native library.

## Build

```
dotnet build Engine/Rin.Audio.Miniaudio/Rin.Audio.Miniaudio.csproj
```

Run `uv run python scripts/pack_native.py Rin.Audio.Miniaudio.Native` (or `uv run python scripts/pack_native.py` for every module) first so the native package is in `.feed/`. There is no test project for it.

## Notes

- Unsafe code is enabled and the project is marked AOT compatible.
