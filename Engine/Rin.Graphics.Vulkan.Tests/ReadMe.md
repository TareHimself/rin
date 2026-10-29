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
| `VulkanExceptionTests.cs` | `ThrowIfFailed` throws on negative `VkResult` codes only |
| `FrameCollectorTests.cs` | Collect hands the newest batch to Execute, a batch the render thread skipped is disposed, renderers with nothing to draw are left out, and a window renderer is found by its window until it is removed |
| `UploadBatchTests.cs` | Filling a flush batch from the write lanes: the per-kind cap, a write whose resource cannot be acquired is dropped and its task cancelled, each resource is acquired once, buffer writes group by handle, staging sizes are aligned |
| `Images/ResourceTableTests.cs` | The generational handle rules: slot 0 is reserved, a freed handle never resolves again, a reused slot gets a newer generation, and only ready occupants resolve. No native calls at all |
| `Windows/ResolvedWindowEventTests.cs` | `Resolve` reads the cursor position for cursor events and the text behind a `TextRef` for text events, and nothing for the rest |
| `Windows/RinWindowTests.cs` | Resolved window events become `RinWindow` events with no native calls, `SetSize` and `SetFullscreen` reach the native window and `IsFullscreen` follows the last call, drag and drop callbacks decode paths and text and keep handler exceptions for the next update |
| `Windows/SystemClipboardTests.cs` | Clipboard text round trips through the native calls |

- A setup fixture redirects the `Rin.Graphics.Vulkan.Native` import to the fake published under `native/Fakes/Rin.Graphics.Vulkan.Native.Fake`, found by searching upward for `rin.sln`. Without it the tests fail with "Rin.Graphics.Vulkan.Native.Fake.dll not published".
- `FakeNative.cs` binds the fake's `fake*` control exports: the next result code, the text behind a text event, a window's fullscreen flag and its drop callbacks. See [native/Fakes/ReadMe.md](../../native/Fakes/ReadMe.md).
- `VulkanGraphicsModule` itself is not started here. It needs a real device, so its startup stays an example-level check.

References: Rin.Graphics.Vulkan (internals visible to this project).
