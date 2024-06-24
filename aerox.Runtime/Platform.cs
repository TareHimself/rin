using System.Runtime.InteropServices;

namespace aerox.Runtime;

public static class Platform
{
    [DllImport(Dlls.AeroxRuntimeNative, EntryPoint = "platformInit", CallingConvention = CallingConvention.Cdecl)]
    private static extern void NativeInit();

    [DllImport(Dlls.AeroxRuntimeNative, EntryPoint = "platformSelectFile", CallingConvention = CallingConvention.Cdecl)]
    private static extern void NativeSelectFile(string title, bool multiple, string filter,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativePathDelegate pathCallback);

    [DllImport(Dlls.AeroxRuntimeNative, EntryPoint = "platformSelectPath", CallingConvention = CallingConvention.Cdecl)]
    private static extern void NativeSelectPath(string title, bool multiple,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativePathDelegate pathCallback);

    public static void Init()
    {
        NativeInit();
    }

    public static Task<string[]> SelectFile(string title = "Select File's", bool multiple = false, string filter = "")
    {
        return Task.Run(() =>
        {
            List<string> results = new();
            NativeSelectFile(title, multiple, filter, p => { results.Add(p); });
            return results.ToArray();
        });
    }

    public static Task<string[]> SelectPath(string title = "Select Path's", bool multiple = false)
    {
        return Task.Run(() =>
        {
            List<string> results = new();
            NativeSelectPath(title, multiple, p => { results.Add(p); });
            return results.ToArray();
        });
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void NativePathDelegate(string path);
}