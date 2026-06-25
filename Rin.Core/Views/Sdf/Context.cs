using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Core.Graphics;

namespace Rin.Core.Views.Sdf;

/// <summary>
///     Generates a MSDF/MTSDF using <a href="https://github.com/Chlumsky/msdfgen">msdfgen</a>
/// </summary>
public class SdfBuilder : IDisposable
{
    private IntPtr _context = Native.sdfContextNew();

    public void Dispose()
    {
        OnDispose();
        GC.SuppressFinalize(this);
    }

    public SdfBuilder BeginContour()
    {
        Native.sdfContextBeginContour(_context);
        return this;
    }

    public SdfBuilder EndContour()
    {
        Native.sdfContextEndContour(_context);
        return this;
    }

    /// <summary>
    ///     Ends the current contour, starts a new contour and moves the position of the cursor
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public SdfBuilder MoveTo(Vector2 point)
    {
        Native.sdfContextMoveTo(_context, ref point);
        return this;
    }

    public SdfBuilder QuadraticBezierTo(Vector2 control, Vector2 point)
    {
        Native.sdfContextQuadraticBezierTo(_context, ref control, ref point);
        return this;
    }


    public SdfBuilder CubicBezierTo(Vector2 control1,
        Vector2 control2,
        Vector2 point)
    {
        Native.sdfContextCubicBezierTo(_context, ref control1, ref control2, ref point);
        return this;
    }

    public SdfBuilder LineTo(Vector2 point)
    {
        Native.sdfContextLineTo(_context, ref point);
        return this;
    }

    /// <summary>
    ///     Stop drawing the vector
    /// </summary>
    /// <returns></returns>
    public SdfBuilder Finish()
    {
        Native.sdfContextFinish(_context);
        return this;
    }

    private class ResultContainer
    {
        public uint Channels = 3;
        public SdfResult? Result = null;
    }
    
    [UnmanagedCallersOnly]
    private static void GenerateCallback(IntPtr data, uint pixelWidth, uint pixelHeight, uint count, double width,
        double height, IntPtr context)
    {
        var handle = GCHandle.FromIntPtr(context);
        if (handle.Target is ResultContainer resultContainer)
        {
            resultContainer.Result = new SdfResult(HostImage.Create(data, pixelWidth, pixelHeight, resultContainer.Channels), width, height);
        }
    }

    public SdfResult? GenerateMSDF(float angleThreshold, float pixelRange)
    {
        unsafe
        {
            var resultContainer = new ResultContainer()
            {
                Channels = 3,
            };
            var handle = GCHandle.Alloc(resultContainer, GCHandleType.Normal);
            try
            {
                Native.sdfContextGenerateMSDF(_context, angleThreshold, pixelRange, &GenerateCallback,
                    GCHandle.ToIntPtr(handle));
            }
            finally
            {
                handle.Free();
            }
            return resultContainer.Result;
        }
    }
    
    public SdfResult? GenerateMTSDF(float angleThreshold, float pixelRange)
    {
        unsafe
        {
            var resultContainer = new ResultContainer()
            {
                Channels = 4
            };
            var handle = GCHandle.Alloc(resultContainer, GCHandleType.Normal);
            try
            {
                Native.sdfContextGenerateMTSDF(_context, angleThreshold, pixelRange,&GenerateCallback,GCHandle.ToIntPtr(handle));
            }
            finally
            {
                handle.Free();
            }
            return resultContainer.Result;
        }
    }

    private void OnDispose()
    {
        if (_context != IntPtr.Zero) Native.sdfContextFree(_context);
        _context = IntPtr.Zero;
    }

    ~SdfBuilder()
    {
        OnDispose();
    }
}