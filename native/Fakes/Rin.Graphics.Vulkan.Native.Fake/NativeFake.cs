using System.Numerics;
using System.Runtime.InteropServices;

namespace Rin.Graphics.Vulkan.Native.Fake;

// Stands in for Rin.Graphics.Vulkan.Native without a GPU or a window system. Windows, allocations and
// the clipboard are bookkeeping only. The fake* exports let a test script the next result code, the
// VulkanInitResult bytes, the text behind a text event, and drive a window's drop callbacks.
public static unsafe class NativeFake
{
    private const int Ok = 0;

    public struct Extent2D
    {
        public uint width;
        public uint height;
    }

    public struct Rect2D
    {
        public int x;
        public int y;
        public uint width;
        public uint height;
    }

    public struct TextRef
    {
        public uint offset;
        public uint length;
    }

    public struct DropCallbacks
    {
        public delegate* unmanaged<void*, void*, nint, uint> enter;
        public delegate* unmanaged<void*, void*, nint, uint> over;
        public delegate* unmanaged<void*, void*, nint, uint> drop;
        public delegate* unmanaged<nint, void> leave;
        public nint userData;
    }

    private sealed class FakeWindow
    {
        public Extent2D Size;
        public Vector2 Cursor;
        public DropCallbacks? Drop;
    }

    private static readonly Dictionary<ulong, FakeWindow> Windows = new();
    private static int _nextResult = Ok;
    private static ulong _nextWindowHandle = 1;
    private static nint _nextAllocation = 1;
    private static int _liveAllocations;
    private static byte[] _initResult = [];
    private static char* _eventText;
    private static int _eventTextLength;
    private static char* _clipboard;

    private static int TakeResult()
    {
        var result = _nextResult;
        _nextResult = Ok;
        return result;
    }

    private static char* CopyUtf16(char* text, int length)
    {
        var copy = (char*)NativeMemory.Alloc((nuint)((length + 1) * sizeof(char)));
        new ReadOnlySpan<char>(text, length).CopyTo(new Span<char>(copy, length));
        copy[length] = '\0';
        return copy;
    }

    // --- test controls ----------------------------------------------------------------------------

    [UnmanagedCallersOnly(EntryPoint = "fakeReset")]
    public static void FakeReset()
    {
        Windows.Clear();
        _nextResult = Ok;
        _nextWindowHandle = 1;
        _nextAllocation = 1;
        _liveAllocations = 0;
        _initResult = [];
        NativeMemory.Free(_eventText);
        _eventText = null;
        _eventTextLength = 0;
        NativeMemory.Free(_clipboard);
        _clipboard = null;
    }

    [UnmanagedCallersOnly(EntryPoint = "fakeFailNext")]
    public static void FakeFailNext(int result) => _nextResult = result;

    [UnmanagedCallersOnly(EntryPoint = "fakeSetInitResult")]
    public static void FakeSetInitResult(byte* bytes, int size) =>
        _initResult = new ReadOnlySpan<byte>(bytes, size).ToArray();

    [UnmanagedCallersOnly(EntryPoint = "fakeWindowCount")]
    public static int FakeWindowCount() => Windows.Count;

    [UnmanagedCallersOnly(EntryPoint = "fakeWindowHasDropCallbacks")]
    public static int FakeWindowHasDropCallbacks(ulong handle) =>
        Windows.TryGetValue(handle, out var window) && window.Drop.HasValue ? 1 : 0;

    [UnmanagedCallersOnly(EntryPoint = "fakeLiveAllocationCount")]
    public static int FakeLiveAllocationCount() => _liveAllocations;

    [UnmanagedCallersOnly(EntryPoint = "fakeSetEventText")]
    public static void FakeSetEventText(char* text, int length)
    {
        NativeMemory.Free(_eventText);
        _eventText = CopyUtf16(text, length);
        _eventTextLength = length;
    }

    [UnmanagedCallersOnly(EntryPoint = "fakeDragEnter")]
    public static uint FakeDragEnter(ulong handle, void* info, void* data) =>
        Windows[handle].Drop is { } callbacks ? callbacks.enter(info, data, callbacks.userData) : 0;

    [UnmanagedCallersOnly(EntryPoint = "fakeDragOver")]
    public static uint FakeDragOver(ulong handle, void* info, void* data) =>
        Windows[handle].Drop is { } callbacks ? callbacks.over(info, data, callbacks.userData) : 0;

    [UnmanagedCallersOnly(EntryPoint = "fakeDrop")]
    public static uint FakeDrop(ulong handle, void* info, void* data) =>
        Windows[handle].Drop is { } callbacks ? callbacks.drop(info, data, callbacks.userData) : 0;

    [UnmanagedCallersOnly(EntryPoint = "fakeDragLeave")]
    public static void FakeDragLeave(ulong handle)
    {
        if (Windows[handle].Drop is { } callbacks) callbacks.leave(callbacks.userData);
    }

    // --- graphics.hpp -----------------------------------------------------------------------------

    [UnmanagedCallersOnly(EntryPoint = "createVulkanInstance")]
    public static int CreateVulkanInstance(ulong windowHandle, byte* outResult)
    {
        if (TakeResult() is var result && result != Ok) return result;
        _initResult.CopyTo(new Span<byte>(outResult, _initResult.Length));
        return Ok;
    }

    [UnmanagedCallersOnly(EntryPoint = "destroyVulkanMessenger")]
    public static void DestroyVulkanMessenger(nint instance, nint messenger)
    {
    }

    [UnmanagedCallersOnly(EntryPoint = "allocatorCreate")]
    public static int AllocatorCreate(nint instance, nint device, nint physicalDevice, nint* outAllocator)
    {
        if (TakeResult() is var result && result != Ok) return result;
        *outAllocator = 0x1000;
        return Ok;
    }

    [UnmanagedCallersOnly(EntryPoint = "allocatorDestroy")]
    public static void AllocatorDestroy(nint allocator)
    {
    }

    [UnmanagedCallersOnly(EntryPoint = "allocatorNewBuffer")]
    public static int AllocatorNewBuffer(nint* buffer, nint* allocation, ulong size, nint allocator,
        int sequentialWrite, int preferHost, int usageFlags, int memoryPropertyFlags, int mapped, byte* debugName)
    {
        if (TakeResult() is var result && result != Ok) return result;
        *buffer = _nextAllocation;
        *allocation = _nextAllocation++;
        _liveAllocations++;
        return Ok;
    }

    [UnmanagedCallersOnly(EntryPoint = "allocatorNewImage")]
    public static int AllocatorNewImage(nint* image, nint* allocation, void* createInfo, nint allocator,
        byte* debugName)
    {
        if (TakeResult() is var result && result != Ok) return result;
        *image = _nextAllocation;
        *allocation = _nextAllocation++;
        _liveAllocations++;
        return Ok;
    }

    [UnmanagedCallersOnly(EntryPoint = "allocatorCopyToBuffer")]
    public static int AllocatorCopyToBuffer(nint allocator, nint allocation, nint data, ulong size, ulong offset) =>
        TakeResult();

    [UnmanagedCallersOnly(EntryPoint = "allocatorSetAllocationName")]
    public static void AllocatorSetAllocationName(nint allocator, nint allocation, byte* name)
    {
    }

    [UnmanagedCallersOnly(EntryPoint = "allocatorFreeBuffer")]
    public static void AllocatorFreeBuffer(nint buffer, nint allocation, nint allocator) => _liveAllocations--;

    [UnmanagedCallersOnly(EntryPoint = "allocatorFreeImage")]
    public static void AllocatorFreeImage(nint image, nint allocation, nint allocator) => _liveAllocations--;

    [UnmanagedCallersOnly(EntryPoint = "dVkCmdBindShadersEXT")]
    public static void DVkCmdBindShadersEXT(nint commandBuffer, uint stageCount, void* stages, void* shaders)
    {
    }

    // --- platform.hpp -----------------------------------------------------------------------------

    [UnmanagedCallersOnly(EntryPoint = "platformWindowPump")]
    public static void PlatformWindowPump()
    {
    }

    [UnmanagedCallersOnly(EntryPoint = "platformWindowCreate")]
    public static int PlatformWindowCreate(byte* title, Extent2D extent, uint flags, ulong* outHandle)
    {
        if (TakeResult() is var result && result != Ok) return result;
        var handle = _nextWindowHandle++;
        Windows[handle] = new FakeWindow { Size = extent };
        *outHandle = handle;
        return Ok;
    }

    [UnmanagedCallersOnly(EntryPoint = "platformWindowDestroy")]
    public static void PlatformWindowDestroy(ulong handle) => Windows.Remove(handle);

    [UnmanagedCallersOnly(EntryPoint = "platformWindowShow")]
    public static void PlatformWindowShow(ulong handle)
    {
    }

    [UnmanagedCallersOnly(EntryPoint = "platformWindowHide")]
    public static void PlatformWindowHide(ulong handle)
    {
    }

    [UnmanagedCallersOnly(EntryPoint = "platformWindowGetCursorPosition")]
    public static Vector2 PlatformWindowGetCursorPosition(ulong handle) => Windows[handle].Cursor;

    [UnmanagedCallersOnly(EntryPoint = "platformWindowSetCursorPosition")]
    public static void PlatformWindowSetCursorPosition(ulong handle, Vector2 position) =>
        Windows[handle].Cursor = position;

    [UnmanagedCallersOnly(EntryPoint = "platformWindowGetSize")]
    public static Extent2D PlatformWindowGetSize(ulong handle) => Windows[handle].Size;

    [UnmanagedCallersOnly(EntryPoint = "platformWindowCreateSurface")]
    public static int PlatformWindowCreateSurface(nint instance, ulong handle, nint* outSurface)
    {
        if (TakeResult() is var result && result != Ok) return result;
        *outSurface = (nint)(0x2000 + handle);
        return Ok;
    }

    [UnmanagedCallersOnly(EntryPoint = "platformWindowGetEvents")]
    public static int PlatformWindowGetEvents(void* output, int size) => 0;

    [UnmanagedCallersOnly(EntryPoint = "platformWindowStartTextInput")]
    public static void PlatformWindowStartTextInput(ulong handle, Rect2D caret)
    {
    }

    [UnmanagedCallersOnly(EntryPoint = "platformWindowStopTextInput")]
    public static void PlatformWindowStopTextInput(ulong handle)
    {
    }

    [UnmanagedCallersOnly(EntryPoint = "platformWindowSetTextInputRect")]
    public static void PlatformWindowSetTextInputRect(ulong handle, Rect2D caret)
    {
    }

    [UnmanagedCallersOnly(EntryPoint = "platformWindowGetEventText")]
    public static char* PlatformWindowGetEventText(TextRef text) =>
        _eventText != null && text.offset + text.length <= (uint)_eventTextLength ? _eventText + text.offset : null;

    [UnmanagedCallersOnly(EntryPoint = "platformWindowSetDropCallbacks")]
    public static void PlatformWindowSetDropCallbacks(ulong handle, DropCallbacks* callbacks) =>
        Windows[handle].Drop = *callbacks;

    [UnmanagedCallersOnly(EntryPoint = "platformWindowClearDropCallbacks")]
    public static void PlatformWindowClearDropCallbacks(ulong handle)
    {
        if (Windows.TryGetValue(handle, out var window)) window.Drop = null;
    }

    [UnmanagedCallersOnly(EntryPoint = "platformHasClipboardText")]
    public static int PlatformHasClipboardText() => _clipboard != null ? 1 : 0;

    [UnmanagedCallersOnly(EntryPoint = "platformGetClipboardText")]
    public static char* PlatformGetClipboardText() => _clipboard;

    [UnmanagedCallersOnly(EntryPoint = "platformSetClipboardText")]
    public static int PlatformSetClipboardText(char* text, ulong length)
    {
        NativeMemory.Free(_clipboard);
        _clipboard = CopyUtf16(text, (int)length);
        return 1;
    }

    [UnmanagedCallersOnly(EntryPoint = "platformClearClipboard")]
    public static void PlatformClearClipboard()
    {
        NativeMemory.Free(_clipboard);
        _clipboard = null;
    }
}
