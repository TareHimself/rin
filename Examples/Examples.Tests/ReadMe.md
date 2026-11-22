# Examples.Tests

NUnit tests for the parts of [Examples](../Examples/ReadMe.md) that have logic worth testing. Folders mirror the example folders.

```
dotnet test Examples/Examples.Tests/Examples.Tests.csproj -p:RinShadeSkipCompile=true
```

| Folder | What it covers |
| --- | --- |
| `AudioPlayer/Library/` | Grouping tracks into albums (album artist, disc and track order, untagged tracks, year, length and cover track) and picking a cover art palette. |
| `AudioPlayer/Playback/` | `MusicLibrary`: albums built from added tracks, one entry per file. |
| `Editor/Model/` | The editor's view of a component: the rows its fields become (lanes for vectors and small number structs, groups for nested structs, read-only rows for derived components), number lanes rounding and clamping integers, rotations as angles, enum choices, the entity hierarchy with its labels, folding and filter, which play commands each play state accepts, and the requirements a component brings or is needed by. |
| `RenderGraphOverlay/Layout/` | The layered render graph layout, built from hand-made snapshots: merged and redundant edges, crossing reduction, no overlap, orthogonal routing, barriers in the gutter and hit testing. |

References: Examples.
