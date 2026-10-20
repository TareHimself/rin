# Examples.Tests

NUnit tests for the parts of [Examples](../Examples/ReadMe.md) that have logic worth testing. Folders mirror the example folders.

```
dotnet test Examples/Examples.Tests/Examples.Tests.csproj -p:RinShadeSkipCompile=true
```

| Folder | What it covers |
| --- | --- |
| `AudioPlayer/Library/` | Grouping tracks into albums (album artist, disc and track order, untagged tracks, year, length and cover track) and picking a cover art palette. |
| `AudioPlayer/Playback/` | `MusicLibrary`: albums built from added tracks, one entry per file. |
| `RenderGraphOverlay/Layout/` | The layered render graph layout, built from hand-made snapshots: merged and redundant edges, crossing reduction, no overlap, orthogonal routing, barriers in the gutter and hit testing. |

References: Examples.
