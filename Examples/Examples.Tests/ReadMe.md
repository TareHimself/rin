# Examples.Tests

NUnit tests for the parts of [Examples](../Examples/ReadMe.md) that have logic worth testing. Folders mirror the example folders.

- `RenderGraphOverlay/Layout/GraphLayoutTests` builds small snapshots by hand and checks the layered layout: edges merged per pass pair, redundant edges, crossing reduction, no overlap in a row, no edge through a pass, horizontal and vertical segments only, barriers in the gutter, and hit testing.

```
dotnet test Examples/Examples.Tests/Examples.Tests.csproj -p:RinShadeSkipCompile=true
```

References: Examples.
