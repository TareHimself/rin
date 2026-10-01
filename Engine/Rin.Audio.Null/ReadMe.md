# Rin.Audio.Null

An audio backend that does nothing, for headless runs and tests. Package id `TareHimself.Rin.Audio.Null`.

## Where it fits

- References: Rin.Core only.
- Nothing else in `Engine/` references it.

## Start here

- `NullAudioModule.cs`: the `IAudioModule` implementation (sealed).
- `NullAudioSample.cs`, `NullActiveAudio.cs`, `NullAudioGroup.cs`, `NullChannel.cs`, `NullPushStream.cs`, `NullEffectController.cs`: no-op implementations of the Rin.Core audio interfaces.

## Build

```
dotnet build Engine/Rin.Audio.Null/Rin.Audio.Null.csproj
```

It has no tests of its own.
