# Rin.Graphics.Vulkan.Tests

NUnit tests for the managed side of [Rin.Graphics.Vulkan](../Rin.Graphics.Vulkan/ReadMe.md)'s native boundary, run against the NativeAOT fake of `Rin.Graphics.Vulkan.Native` instead of the real library. No GPU, window system or Vulkan loader is needed. Folders mirror Rin.Graphics.Vulkan.

Publish the fake first, then test (the order CI uses):

```
uv run python scripts/publish_native_fakes.py win-x64
dotnet test Engine/Rin.Graphics.Vulkan.Tests/Rin.Graphics.Vulkan.Tests.csproj -p:RinShadeSkipCompile=true
```

| File | Covers |
| --- | --- |
| `NativeTests.cs` | Every checked wrapper throws `NativeException` naming the export and code when the native side fails, returns quietly on `Ok`, and `GraphicsResult` and `VulkanInitResult` still match `result.hpp` and `graphics.hpp` |
| `VulkanExceptionTests.cs` | `ThrowIfFailed` throws on negative `VkResult` codes only, as a `VulkanException`, which is a Core `GraphicsException` |
| `FrameCollectorTests.cs` | Collect hands the newest batch to the render thread, a batch it skipped is disposed, renderers with nothing to draw are left out, a renderer removed after the collect is dropped from the batch, and a window renderer is found by its window until it is removed |
| `ResourceLanesTests.cs` | Typed lanes and the shared lane together: merging stops at a fence and at peeked items, a shared write is one fence in each lane it names (one for a copy within a buffer, one per kind for a copy between a buffer and a texture) and once in the shared lane, ordered takes run shared writes in enqueue order with the writes ahead of them and respect the budget, dropping a resource cancels the shared writes that name it |
| `UploadBatchTests.cs` | Filling a flush batch from the lanes: the action cap, an action whose resource cannot be acquired is dropped and its task cancelled, each resource is acquired once, buffer writes group by handle, order across resources joined by a copy is kept, a texture to buffer copy leaves no texture in a transfer layout, staging sizes are aligned |
| `Graph/UploadPlanTests.cs` | Which lane items a graph flushes: segments around each copy in global order, previous-in-lane chains, importing a copy's other side (including the texture of a texture to buffer copy), skipped and blocked copies, and taking a planned prefix leaves the pruned suffix queued |
| `Graph/GraphBuilderTests.cs` | Pruning roots: a pass that writes a `Terminal` resource survives without `ITerminalPass` and keeps what it reads, the rest is pruned and disposed, nothing terminal compiles to null, and `ITerminalPass` still roots. Upload synthesis end to end through `Compile` with real lanes and fake staging and external resources: reads take only their own resource's queued writes, a copy nothing reads is kept by a later kept write of its source, a copy imports its untouched side, a pruned lane suffix stays queued for the next compile, actions enqueued after the peek are not taken, and one staging buffer is allocated and freed with the graph. No device or module involved |
| `Images/ResourceTableTests.cs` | The generational handle rules: slot 0 is reserved, a freed handle never resolves again, a reused slot gets a newer generation, and only ready occupants resolve. No native calls at all |
| `Windows/ResolvedWindowEventTests.cs` | `Resolve` reads the cursor position for cursor events and the text behind a `TextRef` for text events, and nothing for the rest |
| `Windows/RinWindowTests.cs` | Resolved window events become `RinWindow` events with no native calls, `SetSize` and `SetFullscreen` reach the native window and `IsFullscreen` follows the last call, drag and drop callbacks decode paths and text and keep handler exceptions for the next update |
| `Windows/SystemClipboardTests.cs` | Clipboard text round trips through the native calls |

- A setup fixture redirects the `Rin.Graphics.Vulkan.Native` import to the fake published under `native/Fakes/Rin.Graphics.Vulkan.Native.Fake`, found by searching upward for `rin.sln`. Without it the tests fail with "Rin.Graphics.Vulkan.Native.Fake.dll not published".
- `FakeNative.cs` binds the fake's `fake*` control exports: the next result code, the text behind a text event, a window's fullscreen flag and its drop callbacks. See [native/Fakes/ReadMe.md](../../native/Fakes/ReadMe.md).
- `VulkanGraphicsModule` itself is not started here. It needs a real device, so its startup stays an example-level check.

References: Rin.Graphics.Vulkan (internals visible to this project).
