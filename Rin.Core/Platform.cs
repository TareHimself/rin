using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Rin.Core;

public static class Platform
{
    private class ResultContainer
    {
        public readonly List<string> Paths = [];
    }

    [UnmanagedCallersOnly]
    private static unsafe void PlatformSelectCallback(char* path, IntPtr context)
    {
        var handle = GCHandle.FromIntPtr(context);
        if (handle.Target is ResultContainer resultContainer)
        {
            resultContainer.Paths.Add(Marshal.PtrToStringUTF8((nint)path) ?? "");
        }
    }
    [PublicAPI]
    public static string[] SelectFile(string title = "Select File's", bool multiple = false, string filter = "")
    {
        unsafe
        {
            var resultContainer = new ResultContainer();
            var handle = GCHandle.Alloc(resultContainer, GCHandleType.Normal);
            try
            {
                Native.platformSelectFile(title, multiple, filter,&PlatformSelectCallback,GCHandle.ToIntPtr(handle));
            }
            finally
            {
                handle.Free();
            }
            return resultContainer.Paths.ToArray();
        }
    }

    [PublicAPI]
    public static string[] SelectPath(string title = "Select Path's", bool multiple = false)
    {
        unsafe
        {
            var resultContainer = new ResultContainer();
            var handle = GCHandle.Alloc(resultContainer, GCHandleType.Normal);
            try
            {
                Native.platformSelectPath(title, multiple,&PlatformSelectCallback,GCHandle.ToIntPtr(handle));
            }
            finally
            {
                handle.Free();
            }
            return resultContainer.Paths.ToArray();
        }
    }

    public static Task<string[]> SelectFileAsync(string title = "Select File's", bool multiple = false,
        string filter = "")
    {
        return Task.Run(() => SelectFile(title, multiple, filter));
    }

    public static Task<string[]> SelectPathAsync(string title = "Select Path's", bool multiple = false)
    {
        return Task.Run(() => SelectPath(title, multiple));
    }
}