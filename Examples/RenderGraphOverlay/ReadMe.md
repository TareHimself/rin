# RenderGraphOverlay

Debug overlay that draws the render graph of a captured frame on top of a window. A button floats in a corner. Clicking it opens a panel, and Capture in the panel snapshots one frame, so nothing is read every frame. The graph runs top to bottom, one row per execution group. Drag with either mouse button to pan, and scroll to zoom. Click a pass to see what it reads and writes and its private fields, and to highlight the edges around it. The selection stays while you pan and zoom. Clicking the selected pass again or pressing Escape clears it. Fit frames the whole graph, and the Edges button switches between reduced and all edges. It needs no engine changes.

## Using it

Reference the project, then attach it to a window surface after your own content so it draws on top:

```xml
<ProjectReference Include="..\RenderGraphOverlay\RenderGraphOverlay.csproj"/>
```

```csharp
IViewsModule.Get().OnSurfaceCreated += surface =>
{
    surface.Add(myContent);
    RenderGraphOverlay.GraphOverlay.Attach(surface);
};
```

`GraphOverlay.Attach` has overloads taking an `ISurface` (with an optional corner, `new Vector2(1f)` for bottom right by default, and `startOpen`) or an `IWindowRenderer`. It returns the `OverlayView`, or null if the surface is not a window surface. The open panel takes cursor and scroll input, so clicks do not reach the views underneath. While it is closed only the button takes input.

## How a snapshot is taken

1. `GraphSnapshotService` subscribes to `IWindowRenderer.OnCollect`. After Capture is pressed, the next collect adds a one-off `ICollectedData` that puts a `TerminalActionPass` (the probe) into the graph builder.
2. The probe stores the `IGraphConfig` it is given in `Configure`. In `Execute` it receives the compiled graph, and `GraphSnapshotBuilder` reads it.
3. Public data comes from `GraphConfig.ResourceActions`, which lists every read and write of each resource in order. A read after a write becomes a link between the two passes.
4. Private data is read by reflection: `CompiledGraph._nodes` (the execution groups, including the barrier groups), `CompiledGraph._descriptors` (resource sizes and formats) and `GraphBuilder._passes` (to count pruned passes). `PassInspector` reflects over each pass's fields, which is how the internal `BarrierPass` shows its image transitions.
5. The result is a plain `GraphSnapshot` handed to the UI thread, where `InspectorView` picks it up in `Update`.

The probe is left out of the snapshot. Reflection depends on the private field names above, so a rename in the engine shows up as a "Capture failed" message in the panel instead of a crash.

## Files

- `GraphOverlay.cs`: the `Attach` entry points.
- `Snapshot/`: the snapshot records, `GraphSnapshotService` (probe and hand-off), `GraphSnapshotBuilder` (reflection) and `PassInspector` (per-pass fields).
- `Layout/`: a layered layout with no UI types, tested by `RenderGraphOverlay.Tests`. `GraphEdges` merges the per-resource links between two passes into one edge and marks an edge redundant when a longer path already implies it. `LayerOrdering` reorders each row by the average position of its neighbours to cut crossings. `CoordinateAssignment` moves each pass toward the centre of its neighbours without overlapping. `EdgeRouter` routes edges with horizontal and vertical segments, giving overlapping horizontal runs their own tracks. An edge that spans several rows passes through a dummy slot in each row, so it goes between passes and not through them. Redundant edges run through lanes to the right of the graph and show only when selected or with Edges: all. Barriers sit in the left gutter between the rows they separate. `GraphLayout` ties these together.
- `Views/`: `OverlayView` (button and window), `ModalPanelView` (blocks input to the views below), `InspectorView` (toolbar, details panel, blur), `GraphCanvasView` (drawing, selection, pan and zoom), `LegendView`, `DetailsFormatter`, `GraphPalette`.

Tests: `dotnet test Examples/RenderGraphOverlay.Tests/RenderGraphOverlay.Tests.csproj`.

References: Rin.Core, Rin.Graphics.Vulkan. Run it through [RenderGraphViewer](../RenderGraphViewer/ReadMe.md).
