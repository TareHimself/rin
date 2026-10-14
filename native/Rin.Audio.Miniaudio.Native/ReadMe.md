# Rin.Audio.Miniaudio.Native

Native wrapper over miniaudio. Builds `Rin.Audio.Miniaudio.Native` as a shared library and packs it as `TareHimself.Rin.Audio.Miniaudio.Native`.

## Where it fits

- `Engine/Rin.Audio.Miniaudio` references the `TareHimself.Rin.Audio.Miniaudio.Native` package.
- No fake exists for it. CI uses the stub package.

## Start here

- `CMakeLists.txt`: links `miniaudio::miniaudio` privately.
- `silopkg.py`: the silo recipe. Depends on `miniaudio@0.11.22`.
- `src/`: `api.cpp`, `api.hpp`, `macro.hpp`. Each mixer owns an effect node. The game thread publishes a new `EffectList` through `pending`, the audio thread swaps it into `active` and pushes the previous list onto the lock free `retired` stack, and the game thread frees that stack on its next publish, so effect instances are only ever destroyed (and `cleanupCallback` only ever runs) on the game thread. `AudioEffectContext.time` is the time at the start of the block.
- `project.csproj`: shell project used only by `dotnet pack`.

## Build and pack

From the repo root:

```
uv run python scripts/pack_native.py Rin.Audio.Miniaudio.Native
```

For a build to work on, `uv run silo sync native/Rin.Audio.Miniaudio.Native` builds the dependencies and writes the `Rin-Debug` and `Rin-Release` CMake presets, which an IDE can open or `uv run silo run -C native/Rin.Audio.Miniaudio.Native -- cmake --preset Rin-Debug` can configure from this folder. See [../ReadMe.md](../ReadMe.md) for the packing flow.
