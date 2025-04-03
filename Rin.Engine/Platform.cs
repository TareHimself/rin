using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Rin.Engine;

public static class Platform
{
    public static void Init()
    {
        Native.Platform.Init();
    }

    [PublicAPI]
    public static string[] SelectFile(string title = "Select File's", bool multiple = false, string filter = "")
    {
        List<string> results = [];
        unsafe
        {
            Native.Platform.SelectFile(title, multiple, filter,
                p => { results.Add(Marshal.PtrToStringUTF8((nint)p) ?? ""); });
        }

        return results.ToArray();
    }

    [PublicAPI]
    public static string[] SelectPath(string title = "Select Path's", bool multiple = false)
    {
        List<string> results = [];
        unsafe
        {
            Native.Platform.SelectPath(title, multiple,
                p => { results.Add(Marshal.PtrToStringUTF8((nint)p) ?? ""); });
        }

        return results.ToArray();
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