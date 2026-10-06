# RenderGraphOverlay.Tests

NUnit tests for [RenderGraphOverlay](../RenderGraphOverlay/ReadMe.md). `Layout/GraphLayoutTests` builds small snapshots by hand and checks the layered layout: edges merged per pass pair, redundant edges, crossing reduction, no overlap in a row, no edge through a pass, horizontal and vertical segments only, barriers in the gutter, and hit testing.

```
dotnet test Examples/RenderGraphOverlay.Tests/RenderGraphOverlay.Tests.csproj
```

References: RenderGraphOverlay.
