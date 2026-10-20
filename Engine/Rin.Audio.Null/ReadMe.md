# Rin.Audio.Null

An audio backend that does nothing: `NullAudioModule` implements `IAudioModule` with no-op samples, groups, channels, streams and effect controllers, for headless runs and tests. Package id `TareHimself.Rin.Audio.Null`.

```
dotnet build Engine/Rin.Audio.Null/Rin.Audio.Null.csproj
```

It has no tests of its own.

References: Rin.Core. Used by Examples (the `headless` example).
