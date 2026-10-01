# Rin.Audio.Miniaudio.Native

Native wrapper over miniaudio. Builds `Rin.Audio.Miniaudio.Native` as a shared library and packs it as `TareHimself.Rin.Audio.Miniaudio.Native`.

## Where it fits

- `Engine/Rin.Audio.Miniaudio` references the `TareHimself.Rin.Audio.Miniaudio.Native` package.
- No fake exists for it. CI uses the stub package.

## Start here

- `CMakeLists.txt`: links `miniaudio::miniaudio` privately.
- `conanfile.py`: requires `miniaudio/0.11.22`.
- `src/`: `api.cpp`, `api.hpp`, `macro.hpp`.
- `project.csproj`: shell project used only by `dotnet pack`.

## Build and pack

From the repo root:

```
task pack-audio-miniaudio-native
```

Or from this folder, `task build` (Release) or `task buildd` (Debug). See [../ReadMe.md](../ReadMe.md) for the packing flow.
