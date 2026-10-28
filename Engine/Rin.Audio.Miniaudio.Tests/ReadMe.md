# Rin.Audio.Miniaudio.Tests

NUnit tests for [Rin.Audio.Miniaudio](../Rin.Audio.Miniaudio/ReadMe.md). They need no native library: every public type takes an `IMiniaudioEngine`, and the tests hand it the `RecordingEngine` from `TestDoubles.cs`, which logs each call, hands out counter handles and can fail the next call with a scripted `AudioResult`.

```
dotnet test Engine/Rin.Audio.Miniaudio.Tests/Rin.Audio.Miniaudio.Tests.csproj -p:RinShadeSkipCompile=true
```

| File | Covers |
| --- | --- |
| `MiniaudioAudioModuleTests.cs` | Start and stop, master group, volume, loading samples and streams, a failing load |
| `MiniaudioGroupTests.cs` | Mixer volume, play, sub groups, push streams, effect add and remove, dispose order, use after dispose |
| `MiniaudioActiveAudioTests.cs` | Play, pause, seek and volume forwarding, disposal |
| `MiniaudioAudioSampleTests.cs`, `MiniaudioPushStreamTests.cs` | Making a sample active, pushing bytes, disposal |
| `MiniaudioEngineTests.cs` | `AudioResult` matches the enum in `native/Rin.Audio.Miniaudio.Native/src/result.hpp` |

References: Rin.Audio.Miniaudio.
