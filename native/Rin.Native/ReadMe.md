# Rin.Native

Core native helpers for the engine. Builds `Rin.Native` as a shared library and packs it as `TareHimself.Rin.Native`.

## Where it fits

- `Engine/Rin.Core` references the `TareHimself.Rin.Native` package.
- `Engine/Rin.GLTF.Tests` uses the NativeAOT fake of it, see [../Fakes/ReadMe.md](../Fakes/ReadMe.md).

## Start here

- `CMakeLists.txt`: links `webmdx::webmdx` and `msdfgen::msdfgen-core`.
- `silopkg.py`: the silo recipe. Depends on `webmdx@1.0.0` and on `msdfgen@1.12` without its PNG, SVG and command line parts. Both are static libraries, so the package holds `Rin.Native.dll` alone.
- `src/`: `memory.*`, `platform.*`, `sdf.*`, `video.*`, `flags.hpp`, `macro.hpp`.
- `video.*` wraps `wdx::SourceDecoder` for `Rin.Core`'s `WebmVideoPlayer`. A video source is a `VideoSourceCallbacks` struct of managed callbacks mirroring `wdx::ISource`, including `availableFrom`, `makeRangeAvailable` and `releaseBefore`, so a source can hold pieces of the file and be fed while playing. `videoContextSetSource` reads nothing: `videoContextEnsureHeader`, `videoContextDecode` and `videoContextSeek` each return a `VideoDemuxResult`, which is webmdx's `DemuxResult` plus `VIDEO_DEMUX_ERROR` for the exceptions webmdx throws on bad data, caught here so they never unwind into managed code. Track info is copied out under the decoder mutex when the headers are read, so the other getters are safe from any thread. `videoContextGetNeededRange` reports the bytes a step ran short of. `videoContextCopyRecentFrame` writes the frame as NV12, a tightly packed luma plane then an interleaved chroma plane of `ceil(width / 2) * 2` bytes per row, `videoContextGetFrameByteSize` bytes in all; webmdx writes chroma rows with a stride of `width`, so an odd width is repacked here.
- `project.csproj`: shell project used only by `dotnet pack`.

## Build and pack

From the repo root:

```
uv run python scripts/pack_native.py Rin.Native
```

For a build to work on, `uv run silo sync native/Rin.Native` builds the dependencies and writes the `Rin-Debug` and `Rin-Release` CMake presets, which an IDE can open or `uv run silo run -C native/Rin.Native -- cmake --preset Rin-Debug` can configure from this folder. See [../ReadMe.md](../ReadMe.md) for the packing flow.
