# Examples

All the example apps in one executable. Each example is a folder with a class deriving from `Example`, listed in `ExampleRegistry`.

```
dotnet run --project Examples/Examples/Examples.csproj
dotnet run --project Examples/Examples/Examples.csproj -- sponza
```

With no argument a launcher window lists the examples, and clicking one starts it in the same window, which stays at the launcher's 1280x800 because the Vulkan backend cannot resize a window yet. A Back button in the bottom left corner stops the example and returns to the list. With a name as the first argument that example starts directly in a window of its own `WindowSize`. An unknown name prints the list and exits with code 1. `--log-file <path>` redirects stdout and stderr to a file for any example. The examples log through `Log` from Examples.Common (the host logs the font load and each example's start and stop; the audio player logs queued files, unreadable tags, which source a cover came from and failed lookups; Sponza logs its load stages), and the Vulkan validation layer's output lands in the same file.

| Name | Folder | What it shows |
| --- | --- | --- |
| `ui-gallery` | `UiGallery/` | Every quad mode, blur and clipping. |
| `paint` | `Paint/` | Draw on a canvas with the mouse: swatches, brush size, erase, undo. |
| `video-player` | `VideoPlayer/` | A webm video with open, play and pause. |
| `audio-effects` | `AudioEffects/` | An audio file played through switchable effects. |
| `image-strip` | `ImageStrip/` | Tiles that widen on hover: images, a custom shader, a color wheel. |
| `stencil-clip` | `StencilClip/` | Rotating and nested clip regions. |
| `windows` | `ChildWindows/` | Child windows and fullscreen. |
| `node-graph` | `NodeGraphTest/` | Node graph views with typed pins and connections. |
| `p2p-chat` | `P2PChat/` | Peer to peer text chat over TCP. |
| `text-input` | `TextInput/` | Text fields with a readout of caret, composition and code units. |
| `audio-player` | `AudioPlayer/` | A music player with a queue, cover art lookup and a spectrum. |
| `scene-test` | `SceneTest/` | 3D world with physics in a dockable layout. |
| `sponza` | `Sponza/` | `sponza.glb` with a custom Shade mesh material. |
| `asset-viewer` | `AssetViewer/` | A skinned model playing an animation clip. |
| `render-graph` | `RenderGraphViewer/` | The render graph overlay over a small scene. |
| `headless` | `HeadlessTest/` | The engine with no window and no GPU. |

## How it fits together

- `Program.cs` reads the name, then runs `HeadlessApplication` for `headless` or an `ExampleHost` for everything else.
- `ExampleHost` derives from `ExampleApplication` (in [Examples.Common](../Common/ReadMe.md)). It creates one window, registers the `Shaders/Examples` source, loads `assets/fonts/LINESeedJP-Regular.ttf` (LINE Seed JP, SIL OFL, Latin and Japanese) and makes it the views module's `DefaultFontFamily`, so every text view in the examples uses it unless it sets its own. It then starts the example on the first surface. `ExampleHost.AssetPath` resolves a file under `assets/` next to the executable. A child window is disposed when closed, and closing the main window exits.
- `Example` has a `Name` (the argument), a `Title`, a `WindowSize`, `Start(ExampleContext)` and `Stop()`.
- `ExampleContext` carries the host (for `Textures`), the window surface and the command line arguments. An example adds its views with `context.Add` and its handlers with `context.OnUpdate`, `context.OnKey` and `context.OnDrop`, and the host removes and disposes all of them when the example stops. `context.IsActive` turns false at that point, for work that finishes later, such as a model load.
- Stopping removes the views and handlers first, then calls `Stop()`, on Back and on exit. A view frees what it owns in `Dispose` (audio, video, child windows), and `Stop()` frees the rest (worlds, the mesh factory, textures, network sessions). The host puts the audio volumes back to what they were before the example started.
- `Common/Views/` holds what the UI examples share: `Ui` (the palette, and helpers for labels, gaps, rows, columns and buttons), `CardView` (rounded rect with a border and a shadow made of stacked translucent rects), `ActionButton` (hover and pressed colors, centered label), `InputCard` (the border around a text field, which passes clicks on its padding to the field) and `SpacerView`.
- `LauncherView` is the two columns of buttons shown when no name is given, and `BackButtonView` is the Back button.
- `assets/` is copied to the output directory. Shade shaders use paths under `Shaders/Examples/`.

To add an example, add a folder with an `Example` subclass and add it to `ExampleRegistry.All`. Add views and handlers through the context, not the surface or the window, or Back will leave them behind.

## The examples

### ui-gallery

Gallery of UI rendering features: every quad mode, blur and clipping, laid out in sections that wrap to the window width. Scroll by dragging.

### paint

A canvas to draw on. Drag with the left button to paint and with the right button to erase any stroke the brush touches. The swatches pick the colour, the slider sets the brush size from 2 to 48 px, Undo (or Ctrl+Z) reverses the last stroke, erase or clear, and Clear removes everything. `CanvasView` keeps each stroke as its points in canvas space and redraws every stroke each frame as line quads with a circle at each joint, which is a few thousand quads at most and well within what the quad batcher draws in one pass. Nothing is rendered to a texture, so resizing the window keeps the drawing.

### video-player

Open video picks a webm file and plays it fitted to the window. The bar shows the position and length, and its second button plays and pauses. `VideoPlayerView` itself also takes input: a left click on the video toggles play and a right click seeks to that point of its width.

### audio-effects

Open audio picks a wav, ogg, flac or mp3 file and plays it through the master audio group, with a seek bar. Each row adds an effect to that group or removes it, so effects run in the order they were switched on. Master volume starts at 0.5.

- Parametric EQ has six preset buttons. Stress test has a feedback stepper and Bloom a root note stepper (two semitones a step). A value changed while its effect is off is applied when the effect is switched on.
- `Effects/` holds the effects, each a struct marked `[AudioEffect]`: `ParametricEq`, `BassSpiceEffect`, `LowPassEffect`, `StressTestEffect`, `BloomEffect`, `OrbitEffect` and `WarmthEffect`.
- `EffectSlot` adds and removes one effect and keeps its parameters. `Views/` holds `AudioEffectsView` (the screen), `EffectRowView` (name, description, on/off button) and `StepperView`.

### image-strip

A row of tiles in the middle of the window. A tile widens to a square while the cursor is over it (`ExpandOnHoverView`). Add images opens a file picker, and image files dropped on the window are added too. Add shader adds a tile drawn by a custom command (`CustomShaderCommand`, `CustomShaderCommandHandler`) with the Shade shader `PrettyShader`. Add color wheel adds a `CanvasView` that draws a color wheel quad.

### stencil-clip

Three panels with `Clip.Bounds` that spin, one of them nested inside another clipped panel, with children larger than the panel so the clipping shows. There are no input controls.

### windows

One button opens a child window and the other toggles fullscreen on the main window, as does Alt+Enter. A count shows how many child windows are open. Closing a child window disposes it.

### node-graph

Node graph views built from the engine's view system: `GraphView`, node and pin views (text and color picker pins), pin types and connection attempt events.

### p2p-chat

Peer to peer text chat over TCP. One instance hosts on a port, others connect to its address. Start two copies to try it (use `127.0.0.1` on one machine, or the host's LAN address).

On the start screen enter a name, then pick Host a room (port field) or Join a room (address field, `host` or `host:port`, default port 7777). Enter or the Send button sends a message. Leave returns to the start screen. `--preview connect|join|chat` shows a screen with sample content and no networking, for checking the UI.

- `Net/` holds the networking. `ChatHost` accepts any number of peers and relays every chat message to all of them, including the sender, so everyone sees the same order. `ChatClient` connects and sends a hello with its name. Frames are a 4 byte big-endian length followed by UTF-8 JSON (`FrameCodec`). Each session runs on background threads and queues `ChatEvent`s that the example drains on the main thread every frame.
- `Views/` holds the UI: `ConnectView` (start screen with host and join tabs), `ChatRoomView` (status bar, messages in a `ScrollListView` that follows the tail, input row) and `MessageRowView` (name and time above a bubble, own messages on the right). `LineInputView` is a `TextInputBoxView` with a placeholder that sends on Enter, and `StatusDot` is the connection light. The card, buttons, input border and palette come from `Common/Views`.
- There is no NAT traversal, encryption or authentication, so it works on a LAN or loopback only. Names are not checked for duplicates.

### text-input

Two text fields, one single line and one that wraps and takes Enter as a new line, built on `TextInputBoxView`. Click a field to focus it and type. Under the fields a readout shows the focused field, the caret position, the text an input method is composing, and the last 16 UTF-16 units of the content in hex, so a character outside the basic plane shows as its surrogate pair.

A click puts the caret at the nearest character, and text longer than the single line field scrolls sideways with the caret. The multi line field has a fixed height and scrolls vertically, with the caret or the wheel. Escape drops focus.

`ProbeInputView` is the field: it raises `Changed` after every character, composition update, key, click and focus gain. `TextInputDemoView` lays the screen out and fills the readout. The card, field border and palette come from `Common/Views`.

### audio-player

A music player with a queue. Add mp3, wav, flac or ogg files with the plus button on the queue card, drop them on the window, or pass their paths after the example name (`-- audio-player song.mp3`). The current track's cover fills the left column and, blurred, the window behind everything. Under the cover are the title, artist and album, a spectrum of the master output, a seek bar with the times, and the transport: repeat, previous, play and pause, next, mute and volume. Clicking a queue row plays it, and the cross that appears on hover removes it. Under the queue, the equalizer card has six vertical gain sliders (60 Hz to 15 kHz, plus or minus 12 dB, applied while dragging), preset buttons and an on/off switch; it runs the audio-effects example's `ParametricEq` on the master group ahead of the spectrum tap, so the bars show the equalized output. A track that ends starts the next one, and with repeat on the last track goes back to the first. Space plays and pauses, Left restarts the track (or goes to the previous one in its first 3 s) and Right skips. Volume starts at 0.4.

- `Library/` reads files. `TrackInfo` takes the title, artist, album and length from the tags (the `z440.atl.core` package) and splits an `Artist - Title` file name when there is no title tag. `CoverFinder` tries the picture embedded in the tags, then `cover`, `folder`, `front`, `album`, `artwork` or a same-named jpg or png next to the file, then the iTunes Search API, which needs no key.
- `Playback/` holds `Player` (the queue, the one open stream, next and previous, and the move to the next track when the stream stops on its own), `Equalizer` (the always-attached EQ effect, with flat gains while switched off), `QueueEntry` (a track and its cover texture, found on a worker after it is queued), `SpectrumTapEffect` (an `[AudioEffect]` on the master group that passes audio through and writes 40 log-spaced band levels from a 1024 point FFT into a buffer) and `SpectrumAnalyzer` (owns the effect and that buffer and smooths the levels per frame).
- `Views/` holds `PlayerView` (backdrop, blur and the two columns), `NowPlayingView`, `QueueView` and `QueueRowView`, `CoverArtView` (a placeholder tinted from the title until the cover arrives), `EqualizerView`, `SpectrumView`, `SliderView` (horizontal or vertical, with an optional centre origin and live updates), `IconButton` and `Icons`, which loads Font Awesome 6 Free solid from `assets/fonts` when the example starts.

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
context.Add(myContent);
context.Add(GraphOverlay.Create(context.Surface));
```

`GraphOverlay.Create` makes the overlay without adding it, which is what the examples use with `context.Add`. `GraphOverlay.Attach` has overloads taking an `ISurface` (with an optional corner, `new Vector2(1f)` for bottom right by default, and `startOpen`) or an `IWindowRenderer`. It returns the `OverlayView`, or null if the surface is not a window surface. The open panel takes cursor and scroll input, so clicks do not reach the views underneath. While it is closed only the button takes input.

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
