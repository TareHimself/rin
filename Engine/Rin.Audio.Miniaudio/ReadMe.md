# Rin.Audio.Miniaudio

Audio backend built on miniaudio, implementing `IAudioModule` from Rin.Core. Package id `TareHimself.Rin.Audio.Miniaudio`.

## Where it fits

- References: Rin.Core (project reference in `local_file` mode, NuGet in `online` mode) and the `TareHimself.Rin.Audio.Miniaudio.Native` package from the local `.feed/`.
- Nothing else in `Engine/` references it. Applications choose it as their audio module.

## Start here

- `MiniaudioAudioModule.cs`: the module entry point.
- `MiniaudioAudioSample.cs`, `MiniaudioActiveAudio.cs`, `MiniaudioGroup.cs`, `MiniaudioDirectionalBus.cs`, `MiniaudioPushStream.cs`: samples, playback, groups and streams.
- `MiniaudioEffectController.cs`, `NativeEffectManager.cs`: audio effects.
- `Native.cs`: P/Invoke declarations for the native library.

## Build

```
dotnet build Engine/Rin.Audio.Miniaudio/Rin.Audio.Miniaudio.csproj
```

Run `task pack-audio-miniaudio-native` (or `task pack-all`) first so the native package is in `.feed/`. There is no test project for it.

## Notes

- Unsafe code is enabled and the project is marked AOT compatible.
