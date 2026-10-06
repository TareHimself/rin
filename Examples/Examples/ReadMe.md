# Examples

All the example apps in one executable. Each example is a folder with a class deriving from `Example`, listed in `ExampleRegistry`.

```
dotnet run --project Examples/Examples/Examples.csproj
dotnet run --project Examples/Examples/Examples.csproj -- sponza
```

With no argument a launcher window lists the examples, and clicking one starts it in the same window, which stays at the launcher's 1280x800 because the Vulkan backend cannot resize a window yet. With a name as the first argument that example starts directly in a window of its own `WindowSize`. An unknown name prints the list and exits with code 1. `--log-file <path>` redirects stdout and stderr to a file for any example.

| Name | Folder | What it shows |
| --- | --- | --- |
| `ui-gallery` | `UiGallery/` | Every quad mode, blur and clipping. |
| `views-test` | `ViewsTest/` | Views, animation, images and audio effects. |
| `node-graph` | `NodeGraphTest/` | Node graph views with typed pins and connections. |
| `p2p-chat` | `P2PChat/` | Peer to peer text chat over TCP. |
| `audio-player` | `AudioPlayer/` | Audio player UI over an image switcher. |
| `scene-test` | `SceneTest/` | 3D world with physics in a dockable layout. |
| `sponza` | `Sponza/` | `sponza.glb` with a custom Shade mesh material. |
| `asset-viewer` | `AssetViewer/` | A skinned model playing an animation clip. |
| `render-graph` | `RenderGraphViewer/` | The render graph overlay over a small scene. |
| `headless` | `HeadlessTest/` | The engine with no window and no GPU. |

## How it fits together

- `Program.cs` reads the name, then runs `HeadlessApplication` for `headless` or an `ExampleHost` for everything else.
- `ExampleHost` derives from `ExampleApplication` (in [Examples.Common](../Common/ReadMe.md)). It creates one window, registers the `Shaders/Examples` source, and starts the example on the first surface. A child window is disposed when closed, and closing the main window exits.
- `Example` has a `Name` (the argument), a `Title`, a `WindowSize`, `Start(ExampleContext)` and `Stop()`. `ExampleContext` carries the host (for `OnUpdate` and `Textures`), the window surface and the command line arguments.
- `LauncherView` is the list of buttons shown when no name is given.
- `assets/` is copied to the output directory. Shade shaders use paths under `Shaders/Examples/`.

To add an example, add a folder with an `Example` subclass and add it to `ExampleRegistry.All`.

## The examples

### ui-gallery

Gallery of UI rendering features: every quad mode, blur and clipping, laid out in sections that wrap to the window width. Scroll by dragging.

### views-test

Test bench for views, animation, images and audio effects (parametric EQ, stress-test delay, bloom). `--stencil` shows `StencilScene` instead of the default animation scene.

- Up opens a child window. Alt+Enter toggles fullscreen.
- Default scene: `=`, `-`, `0` add test items to the list.
- K toggles the parametric EQ (then M or N pick the vocal or bass preset), J toggles the stress-test effect (then H and Y lower and raise feedback), L toggles the bloom effect.

### node-graph

Node graph views built from the engine's view system: `GraphView`, node and pin views (text and color picker pins), pin types and connection attempt events.

### p2p-chat

Peer to peer text chat over TCP. One instance hosts on a port, others connect to its address. Start two copies to try it (use `127.0.0.1` on one machine, or the host's LAN address).

On the start screen enter a name, then pick Host a room (port field) or Join a room (address field, `host` or `host:port`, default port 7777). Enter or the Send button sends a message. Leave returns to the start screen. `--preview connect|join|chat` shows a screen with sample content and no networking, for checking the UI.

- `Net/` holds the networking. `ChatHost` accepts any number of peers and relays every chat message to all of them, including the sender, so everyone sees the same order. `ChatClient` connects and sends a hello with its name. Frames are a 4 byte big-endian length followed by UTF-8 JSON (`FrameCodec`). Each session runs on background threads and queues `ChatEvent`s that the example drains on the main thread every frame.
- `Views/` holds the UI: `ConnectView` (start screen with host and join tabs), `ChatRoomView` (status bar, messages in a `ScrollListView` that follows the tail, input row) and `MessageRowView` (name and time above a bubble, own messages on the right). Building blocks: `CardView` (rounded rect with border and a shadow made of stacked translucent rects), `ActionButton` (hover and pressed colors, centered label), `InputCard` and `LineInputView` (a `TextInputBoxView` with a placeholder that sends on Enter), `StatusDot`, `SpacerView` and the palette in `Ui`.
- There is no NAT traversal, encryption or authentication, so it works on a LAN or loopback only. Names are not checked for duplicates.

### audio-player

Audio player UI (track player, visualizer, file picker views) over an image switcher background, built on the engine's views and Miniaudio modules. Uses the `SpotifyExplode` and `YoutubeExplode` packages. Volume starts at 0.1.

Left and Right arrows change the background image, Enter opens a file picker for png/jpg images.

### scene-test

3D world with Bepu physics shown in a dockable layout (`DockSpaceView` from `experiments.Docking`) with a perspective camera and a top camera. Uses the glTF models and textures in `assets/`.

- W, A, S, D move the camera in the viewport. P drops a grid of 2000 boxes.
- The "Render graph" button in the bottom-right corner opens the render graph overlay.

### sponza

Loads `assets/models/sponza.glb` on a background task, then shows it through a viewport with a point-light camera, a custom Shade-based mesh material (`SponzaMeshMaterial`) and an FPS counter in the top right.

W, A, S, D move the camera while the viewport is focused, mouse movement turns it (from `TestViewport.cs`).

### asset-viewer

Loads the skinned `assets/models/fox.glb` model, places the camera to frame its bounds and loops the "Run" animation clip. Model path, clip name and camera framing are hardcoded in `AssetViewerExample.cs`. There are no input controls.

### render-graph

A window with a little content and the render graph overlay attached and open. Press Capture to snapshot a frame. The graph it shows is the one that draws the viewer itself.

### headless

Runs the engine with `NullGraphicsModule` and `NullAudioModule` (no window, no GPU). Creates a `World` with Bepu physics, drops a sphere from height 50 at an accelerated time scale, prints its height every 0.25 s and exits after 3 s. It has its own `Application` class, so it is not in the launcher.

## The render graph overlay

`RenderGraphOverlay/` is a debug overlay that draws the render graph of a captured frame on top of a window. A button floats in a corner. Clicking it opens a panel, and Capture in the panel snapshots one frame, so nothing is read every frame. The graph runs top to bottom, one row per execution group. Drag with either mouse button to pan, and scroll to zoom. Click a pass to see what it reads and writes and its private fields, and to highlight the edges around it. The selection stays while you pan and zoom. Clicking the selected pass again or pressing Escape clears it. Fit frames the whole graph, and the Edges button switches between reduced and all edges. It needs no engine changes.

Attach it to a window surface after the example's own content so it draws on top:

```csharp
context.Surface.Add(myContent);
GraphOverlay.Attach(context.Surface);
```

`GraphOverlay.Attach` has overloads taking an `ISurface` (with an optional corner, `new Vector2(1f)` for bottom right by default, and `startOpen`) or an `IWindowRenderer`. It returns the `OverlayView`, or null if the surface is not a window surface. The open panel takes cursor and scroll input, so clicks do not reach the views underneath. While it is closed only the button takes input.

How a snapshot is taken:

1. `GraphSnapshotService` subscribes to `IWindowRenderer.OnCollect`. After Capture is pressed, the next collect adds a one-off `ICollectedData` that puts a `TerminalActionPass` (the probe) into the graph builder.
2. The probe stores the `IGraphConfig` it is given in `Configure`. In `Execute` it receives the compiled graph, and `GraphSnapshotBuilder` reads it.
3. Public data comes from `GraphConfig.ResourceActions`, which lists every read and write of each resource in order. A read after a write becomes a link between the two passes.
4. Private data is read by reflection: `CompiledGraph._nodes` (the execution groups, including the barrier groups), `CompiledGraph._descriptors` (resource sizes and formats) and `GraphBuilder._passes` (to count pruned passes). `PassInspector` reflects over each pass's fields, which is how the internal `BarrierPass` shows its image transitions.
5. The result is a plain `GraphSnapshot` handed to the UI thread, where `InspectorView` picks it up in `Update`.

The probe is left out of the snapshot. Reflection depends on the private field names above, so a rename in the engine shows up as a "Capture failed" message in the panel instead of a crash.

Files:

- `GraphOverlay.cs`: the `Attach` entry points.
- `Snapshot/`: the snapshot records, `GraphSnapshotService` (probe and hand-off), `GraphSnapshotBuilder` (reflection) and `PassInspector` (per-pass fields).
- `Layout/`: a layered layout with no UI types, tested by [Examples.Tests](../Examples.Tests/ReadMe.md). `GraphEdges` merges the per-resource links between two passes into one edge and marks an edge redundant when a longer path already implies it. `LayerOrdering` reorders each row by the average position of its neighbours to cut crossings. `CoordinateAssignment` moves each pass toward the centre of its neighbours without overlapping. `EdgeRouter` routes edges with horizontal and vertical segments, giving overlapping horizontal runs their own tracks. An edge that spans several rows passes through a dummy slot in each row, so it goes between passes and not through them. Redundant edges run through lanes to the right of the graph and show only when selected or with Edges: all. Barriers sit in the left gutter between the rows they separate. `GraphLayout` ties these together.
- `Views/`: `OverlayView` (button and window), `ModalPanelView` (blocks input to the views below), `InspectorView` (toolbar, details panel, blur), `GraphCanvasView` (drawing, selection, pan and zoom), `LegendView`, `DetailsFormatter`, `GraphPalette`.

References: Examples.Common, Rin.Core, Rin.World, Rin.GLTF, Rin.Graphics.Null, Rin.Audio.Null, Rin.Shade (plus its source generator), Rin.SourceGenerators, experiments.Docking.
