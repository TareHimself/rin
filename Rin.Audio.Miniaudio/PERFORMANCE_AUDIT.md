# Rin.Audio.Miniaudio — Performance Audit

## Summary

| # | Severity | Issue | File | Lines |
|---|----------|-------|------|-------|
| 1 | CRITICAL | Race condition on effect parameter writes | `MiniaudioEffectController.cs` | 43–44 |
| 2 | CRITICAL | GCHandle type mismatch risk | `NativeEffectManager.cs` | 23 |
| 3 | HIGH | Per-property P/Invoke in hot path | `MiniaudioActiveAudio.cs` | 14–17 |
| 4 | HIGH | `ReadAll` allocates full heap copy via intermediate `MemoryStream` | `MiniaudioAudioModule.cs` | 52–57 |
| 5 | MEDIUM | `Dictionary` not thread-safe | `MiniaudioGroup.cs` | 12 |
| 6 | MEDIUM | Three atomic exchanges every audio callback, even with no pending changes | `api.cpp` | 107–111 |
| 7 | MEDIUM | 2–3 `memcpy` per effect per audio callback | `api.cpp` | 116–133 |
| 8 | MEDIUM | `memmove` compaction on every push-stream write | `api.cpp` | 706–713 |

---

## Critical Issues

### 1. Race condition: effect parameter writes

**File:** `MiniaudioEffectController.cs:43–44`

```csharp
var paramsPtr = (TParams*)_parametersPtr;
*paramsPtr = value;   // audio thread reads this concurrently — no fence
```

The audio callback (`effectNode_process` in `api.cpp:121–125`) reads `inst->effect.parameters` on the real-time audio thread with no synchronization. The managed property setter writes to the same memory on the caller thread (typically the UI or game thread) with no memory barrier.

For `TParams` structs larger than a native pointer this is a **torn write** — the audio thread can read a partially-written struct, which causes undefined behavior (clicks, crashes, corrupted parameters).

**Recommended fix:**
```csharp
unsafe {
    if (_disposed || _parametersPtr == IntPtr.Zero) return;
    field = value;
    Unsafe.Write((void*)_parametersPtr, value);
    Thread.MemoryBarrier();   // publish before audio thread reads
}
```
For structs larger than 8 bytes the only fully safe solution is a native-side double-buffer: the setter writes to a "pending" slot and the audio callback swaps atomically, mirroring the existing `pendingEffects` pattern in `EffectManager`.

---

### 2. GCHandle lifecycle risk

**File:** `NativeEffectManager.cs:23`

```csharp
GCHandle.Alloc(descriptor, GCHandleType.Normal)
```

`GCHandleType.Normal` prevents the GC from collecting `descriptor` but does **not** pin it. The `IntPtr` handle is passed through native code and later resolved via `GCHandle.FromIntPtr` in `HandleReleaseEffect`. This part is safe — the handle table tracks the object regardless of movement.

The risk is in the `Parameters` and `State` `IntPtr` fields returned by `descriptor.CreateParameters()` / `CreateState()`. These are raw unmanaged pointers. If any implementation allocates managed memory and returns a pointer into it (e.g., a pinned array that gets unpinned early), or fails to match `Create*` with `Release*`, this becomes a use-after-free.

**Recommended fix:**  
Audit every `IAudioEffectDescriptor` implementation to confirm:
- `CreateParameters` / `CreateState` allocate with `NativeMemory.Alloc` (or equivalent unmanaged allocator), not managed arrays.
- `ReleaseParameters` / `ReleaseState` always free with the matching deallocator.
- Add a `#if DEBUG` assertion in `HandleReleaseEffect` that verifies the `parameters` and `state` pointers are non-null before freeing.

---

## High Issues

### 3. Per-property P/Invoke in hot path

**File:** `MiniaudioActiveAudio.cs:14–17`

```csharp
public bool IsPlaying => Native.audioActiveIsPlaying(Id) != 0;
public double Position => Native.audioActiveGetPosition(Id);
public double Duration => Native.audioActiveGetDuration(Id);
```

Each property read crosses the P/Invoke boundary, acquires `g_mutex` (a global `std::mutex` shared with all audio operations), performs a `std::unordered_map` lookup, and returns. Any caller polling these every frame — for example, a UI progress bar or a "waiting for playback to finish" loop — causes:

- Multiple mutex acquisitions per frame on the calling thread.
- Serialization: the audio thread cannot create, destroy, or modify any sound while these locks are held.
- ~5–15 µs overhead per call on Windows under contention.

**Recommended fix:**  
Add a single batch query on the native side:

```c
// api.cpp — new function
void audioActiveGetState(uint64_t id, int* isPlaying, double* position, double* duration);
```

```csharp
// MiniaudioActiveAudio.cs
public void RefreshState(out bool isPlaying, out double position, out double duration) {
    int playing; double pos, dur;
    Native.audioActiveGetState(Id, &playing, &pos, &dur);
    isPlaying = playing != 0; position = pos; duration = dur;
}
```

Callers that need only `IsPlaying` or `Duration` once (e.g., `await until finished`) can keep the existing properties. Callers that poll every frame should use `RefreshState`.

---

### 4. `ReadAll` double-allocates file contents

**File:** `MiniaudioAudioModule.cs:52–57`

```csharp
private static ReadOnlySpan<byte> ReadAll(Stream stream) {
    using var ms = new MemoryStream();
    stream.CopyTo(ms);
    return ms.ToArray();    // second copy: MemoryStream internal buffer → new array
}
```

When loading a Stream-backed audio sample, the file is read into `MemoryStream`'s internal buffer, then copied again via `ToArray()`. The returned span wraps the second array — the first buffer is immediately discarded. For a 10 MB audio file this allocates ~20 MB transiently and adds a full GC generation pressure spike.

Additionally, returning a `ReadOnlySpan<byte>` from a method that allocates a `byte[]` is misleading — the span's backing array must remain rooted by the caller, which it does implicitly since the callers pass it into `fixed`, but this is fragile.

**Recommended fix:**

```csharp
private static byte[] ReadAll(Stream stream)
{
    if (stream.CanSeek)
    {
        var buf = new byte[stream.Length - stream.Position];
        stream.ReadExactly(buf);
        return buf;     // single allocation, exact size
    }
    using var ms = new MemoryStream();
    stream.CopyTo(ms);
    return ms.ToArray();
}
```

Change callers to accept `byte[]` directly. `MiniaudioAudioSample.FromMemory(ReadOnlySpan<byte>)` accepts a span, which `byte[]` implicitly converts to — no call-site changes needed.

---

## Medium Issues

### 5. `Dictionary` not thread-safe

**File:** `MiniaudioGroup.cs:12`

```csharp
private readonly Dictionary<ulong, IEffectController> _effectControllers = [];
```

`AddEffect`, `RemoveEffect`, and `OnEffectRemoved` all access this dictionary. If these methods are called concurrently (e.g., a background asset-loader adds an effect while the main thread disposes the group), the dictionary can corrupt its internal state.

**Recommended fix:**  
Replace with `ConcurrentDictionary<ulong, IEffectController>` and use `TryAdd` / `TryRemove` in place of `Add` / `Remove`. The existing `TryGetValue` in `RemoveEffect` maps directly to `TryGetValue` on `ConcurrentDictionary` without change.

---

### 6. Three atomic exchanges every audio callback

**File:** `native/Rin.Audio.Miniaudio.Native/src/api.cpp:107–111`

```cpp
if (auto* pending = node->effectManager->pendingEffects.exchange(nullptr))
{
    auto* stale = node->effectManager->activeEffects.exchange(pending);
    node->effectManager->staleEffects.exchange(stale);
}
```

This runs on every invocation of `effectNode_process` — the real-time audio callback. The outer `if` correctly short-circuits when there is no pending update, but `exchange(nullptr)` itself is a full read-modify-write atomic operation (sequentially consistent by default on x86, but an explicit `memory_order_acq_rel` would make intent clearer and avoid a fence on ARM).

The previous value written to `staleEffects` by the `exchange` is silently discarded — the old stale vector leaks until it is overwritten by a subsequent update. Verify that `AddEffect` / `RemoveEffect` delete the old stale pointer before writing a new one.

**Recommended fix:**  
Use `memory_order_acquire` on the check load to avoid an unnecessary fence on the fast path:

```cpp
if (node->effectManager->pendingEffects.load(std::memory_order_acquire) != nullptr)
{
    auto* pending = node->effectManager->pendingEffects.exchange(nullptr, std::memory_order_acq_rel);
    if (pending) {
        auto* stale = node->effectManager->activeEffects.exchange(pending, std::memory_order_acq_rel);
        auto* old = node->effectManager->staleEffects.exchange(stale, std::memory_order_acq_rel);
        delete old;   // free the vector that was already stale-from-last-update
    }
}
```

---

### 7. 2–3 `memcpy` per effect per audio callback

**File:** `native/Rin.Audio.Miniaudio.Native/src/api.cpp:116–133`

```cpp
memcpy(node->scratch.data(), ppFramesIn[0], sampleCount * sizeof(float));   // (1)
for (const auto& inst : *active) {
    inst->effect.processCallback(node->scratch.data(), ppFramesOut[0], ...);
    memcpy(node->scratch.data(), ppFramesOut[0], sampleCount * sizeof(float)); // (2) ping-pong
}
// passthrough (no effects):
memcpy(ppFramesOut[0], ppFramesIn[0], frameCount * channels * sizeof(float)); // (3)
```

With a single effect (the common case): 2 copies of the entire audio frame (copy in → scratch, copy out → scratch). With N effects: 1 + N copies. At 48 kHz stereo, 512 frames per callback, each copy is 4 KB — this is ~8 KB of memory traffic per callback for the single-effect case, plus cache pollution.

**Recommended fix:**  
Use the output buffer as the initial destination and ping-pong between `ppFramesOut[0]` and `scratch` in place:

```cpp
// First effect reads from input, writes to output.
// Subsequent effects read from output, write to scratch, then swap.
// This eliminates copy (1) entirely and reduces copy (2) to a pointer swap.
```

For the passthrough case (`ppFramesIn[0]` → `ppFramesOut[0]`), check whether miniaudio can configure the node with `MA_NODE_FLAG_PASSTHROUGH` when the effect list is empty, which lets the engine skip the copy at the graph level.

---

### 8. `memmove` compaction on every push-stream write

**File:** `native/Rin.Audio.Miniaudio.Native/src/api.cpp:706–713`

```cpp
if (src->readPos > 0) {
    const std::size_t remaining = src->buffer.size() - src->readPos;
    if (remaining > 0)
        memmove(src->buffer.data(), src->buffer.data() + src->readPos, remaining);
    src->buffer.resize(remaining);  // may trigger reallocation
    src->readPos = 0;
}
src->buffer.insert(src->buffer.end(), data, data + size);
```

Every call to `audioPushStreamPush` (which is on the application thread, holding both `g_mutex` and `src->mutex`) shifts all unconsumed audio data to the front of the vector. For a 48 kHz stereo `float32` stream with 100 ms of buffering, the `memmove` moves ~38 KB on every push. The `resize` can also trigger a `realloc` + copy if the remaining size shrinks past a threshold and then grows again.

**Recommended fix:**  
Replace `std::vector<uint8_t> buffer` + `size_t readPos` with a fixed-capacity ring buffer:

```cpp
struct PushStreamSource {
    // ...
    std::vector<uint8_t> buffer;   // pre-allocated to capacity
    std::size_t head{0};           // read position
    std::size_t tail{0};           // write position
    std::size_t count{0};          // bytes in use
    // ...
};
```

Push appends at `tail` (modulo capacity), read advances `head` — both are O(1) with no data movement. Pre-allocate to `sampleRate * channels * bytesPerSample * N` seconds of buffer during `audioPushStreamCreate`.

---

## Verification Checklist

- [ ] Build `Rin.Audio.Miniaudio` with no errors after any fix: `dotnet build`
- [ ] Run `Rin.Core.Tests` — confirm no regressions in audio interfaces
- [ ] Profile push-stream path with dotnet-trace or VS Diagnostics; confirm no per-push allocation spikes
- [ ] Run native code under AddressSanitizer to detect use-after-free in effect parameter writes
- [ ] Confirm `ReadAll` no longer creates an intermediate `MemoryStream` for seekable streams (check with Memory Profiler snapshot)
- [ ] Verify stale-effect vector is always freed (no leak) after the atomic triple-exchange fix
