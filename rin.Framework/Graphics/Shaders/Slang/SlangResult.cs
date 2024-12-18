using System.Runtime.InteropServices;

namespace rin.Framework.Graphics.Shaders.Slang;

public class SlangResult : IDisposable
{

    private readonly unsafe void* _result;
    
    public unsafe SlangResult(void* result)
    {
        _result = result;
    }

    public bool HasResult => GetResultSize() != 0;

    public int GetDiagnosticsCount()
    {
        unsafe
        {
            return NativeMethods.SlangResultGetDiagnosticsCount(_result);
        }
    }

    public string GetDiagnostic(int index)
    {
        unsafe
        {
            return Marshal.PtrToStringAuto(new IntPtr(NativeMethods.SlangResultGetDiagnostic(_result, index))) ?? string.Empty;
        }
    }

    public unsafe void* GetResultPointer()
    {
        unsafe
        {
            return NativeMethods.SlangResultGetPointer(_result);
        }
    }

    public int GetResultSize()
    {
        unsafe
        {
            return NativeMethods.SlangResultGetSize(_result);
        }
    }
    
    private void ReleaseUnmanagedResources()
    {
        unsafe
        {
            NativeMethods.SlangResultDestroy(_result);
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~SlangResult()
    {
        ReleaseUnmanagedResources();
    }
}