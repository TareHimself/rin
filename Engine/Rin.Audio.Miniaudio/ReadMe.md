# Rin.Audio.Miniaudio

The miniaudio audio backend: `MiniaudioAudioModule` implements `IAudioModule` from Rin.Core over the native library in the `TareHimself.Rin.Audio.Miniaudio.Native` package. Package id `TareHimself.Rin.Audio.Miniaudio`.

```
uv run python scripts/pack_native.py Rin.Audio.Miniaudio.Native
dotnet build Engine/Rin.Audio.Miniaudio/Rin.Audio.Miniaudio.csproj
```

The first command packs the native package into the local `.feed/` folder the build restores from (leave the module name out to pack every module). There is no test project.

Native calls that create something go through the checked wrappers in `Native.cs` (`Native.Init`, `Native.MakeSampleFromFile`, `Native.MixerCreate` and so on), which throw `NativeException` with the export's name and its `AudioResult` code instead of handing back a zero handle.

References: Rin.Core (project reference in `local_file` mode, the `TareHimself.Rin.Core` package in `online` mode), TareHimself.Rin.Audio.Miniaudio.Native. Used by Examples.Common and the projects under `Experiments/`.
