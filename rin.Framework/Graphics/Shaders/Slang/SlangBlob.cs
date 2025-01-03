﻿namespace rin.Framework.Graphics.Shaders.Slang;

public class SlangBlob : IDisposable
{
    private readonly unsafe void * _ptr;

    public unsafe SlangBlob()
    {
        _ptr = NativeMethods.SlangBlobNew();
    }
    
    public unsafe SlangBlob(void * ptr)
    {
        _ptr = ptr;
    }

    public unsafe void* ToPointer() => _ptr;

    public int GetSize()
    {
        unsafe
        {
            return NativeMethods.SlangBlobGetSize(_ptr);
        }
    }
    
    public IntPtr GetDataPointer()
    {
        unsafe
        {
            return new IntPtr(NativeMethods.SlangBlobGetPointer(_ptr));
        }
    }
    private void ReleaseUnmanagedResources()
    {
        unsafe
        {
            NativeMethods.SlangBlobFree(_ptr);
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~SlangBlob()
    {
        ReleaseUnmanagedResources();
    }
}