using System.Runtime.InteropServices;

namespace Rin.Graphics.Vulkan.Tests;

/// <summary>
///     The test-only exports of the Rin.Graphics.Vulkan.Native fake.
/// </summary>
internal static partial class FakeNative
{
    public const string DllName = "Rin.Graphics.Vulkan.Native.Fake";

    [LibraryImport(DllName, EntryPoint = "fakeReset")]
    public static partial void Reset();

    [LibraryImport(DllName, EntryPoint = "fakeFailNext")]
    public static partial void FailNext(Native.GraphicsResult result);

    [LibraryImport(DllName, EntryPoint = "fakeSetInitResult")]
    private static unsafe partial void SetInitResult(byte* bytes, int size);

    [LibraryImport(DllName, EntryPoint = "fakeWindowCount")]
    public static partial int WindowCount();

    [LibraryImport(DllName, EntryPoint = "fakeWindowHasDropCallbacks")]
    public static partial int WindowHasDropCallbacks(ulong handle);

    [LibraryImport(DllName, EntryPoint = "fakeWindowIsFullscreen")]
    public static partial int WindowIsFullscreen(ulong handle);

    [LibraryImport(DllName, EntryPoint = "fakeLiveAllocationCount")]
    public static partial int LiveAllocationCount();

    [LibraryImport(DllName, EntryPoint = "fakeSetEventText")]
    private static unsafe partial void SetEventText(char* text, int length);

    [LibraryImport(DllName, EntryPoint = "fakeDragEnter")]
    public static unsafe partial uint DragEnter(ulong handle, Native.Window.DropInfo* info,
        Native.Window.DropData* data);

    [LibraryImport(DllName, EntryPoint = "fakeDragOver")]
    public static unsafe partial uint DragOver(ulong handle, Native.Window.DropInfo* info,
        Native.Window.DropData* data);

    [LibraryImport(DllName, EntryPoint = "fakeDrop")]
    public static unsafe partial uint Drop(ulong handle, Native.Window.DropInfo* info, Native.Window.DropData* data);

    [LibraryImport(DllName, EntryPoint = "fakeDragLeave")]
    public static partial void DragLeave(ulong handle);

    public static unsafe void SetEventText(string text)
    {
        fixed (char* chars = text)
        {
            SetEventText(chars, text.Length);
        }
    }

    public static unsafe void SetInitResult(ReadOnlySpan<byte> bytes)
    {
        fixed (byte* pointer = bytes)
        {
            SetInitResult(pointer, bytes.Length);
        }
    }
}
