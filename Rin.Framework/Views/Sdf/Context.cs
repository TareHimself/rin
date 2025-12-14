using System.Numerics;
using Rin.Framework.Graphics;

namespace Rin.Framework.Views.Sdf;

/// <summary>
/// Generates a MSDF/MTSDF using <a href="https://github.com/Chlumsky/msdfgen">msdfgen</a>
/// </summary>
public class SdfBuilder : IDisposable
{
    private IntPtr _context = Native.Sdf.ContextNew();

    public void Dispose()
    {
        OnDispose();
        GC.SuppressFinalize(this);
    }
    
    public SdfBuilder BeginContour()
    {
        Native.Sdf.ContextBeginContour(_context);
        return this;
    }
    
    public SdfBuilder EndContour()
    {
        Native.Sdf.ContextEndContour(_context);
        return this;
    }

    /// <summary>
    /// Ends the current contour, starts a new contour and moves the position of the cursor
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public SdfBuilder MoveTo(Vector2 point)
    {
        Native.Sdf.ContextMoveTo(_context, ref point);
        return this;
    }

    public SdfBuilder QuadraticBezierTo(Vector2 control, Vector2 point)
    {
        Native.Sdf.ContextQuadraticBezierTo(_context, ref control, ref point);
        return this;
    }


    public SdfBuilder CubicBezierTo(Vector2 control1,
        Vector2 control2,
        Vector2 point)
    {
        Native.Sdf.ContextCubicBezierTo(_context, ref control1, ref control2, ref point);
        return this;
    }

    public SdfBuilder LineTo(Vector2 point)
    {
        Native.Sdf.ContextLineTo(_context, ref point);
        return this;
    }

    /// <summary>
    /// Stop drawing the vector
    /// </summary>
    /// <returns></returns>
    public SdfBuilder Finish()
    {
        Native.Sdf.ContextFinish(_context);
        return this;
    }
    
    public SdfResult? GenerateMSDF(float angleThreshold, float pixelRange)
    {
        SdfResult? result = null;

        Native.Sdf.ContextGenerateMsdf(_context, angleThreshold, pixelRange,
            (data, pixelWidth, pixelHeight, count, width, height) =>
            {
                result = new SdfResult(HostImage.Create(data, pixelWidth, pixelHeight, 3), width, height);
            });

        return result;
    }

    public SdfResult? GenerateMTSDF(float angleThreshold, float pixelRange)
    {
        SdfResult? result = null;

        Native.Sdf.ContextGenerateMtsdf(_context, angleThreshold, pixelRange,
            (data, pixelWidth, pixelHeight, count, width, height) =>
            {
                result = new SdfResult(HostImage.Create(data, pixelWidth, pixelHeight, 4), width, height);
            });


        return result;
    }

    private void OnDispose()
    {
        if (_context != IntPtr.Zero) Native.Sdf.ContextFree(_context);
        _context = IntPtr.Zero;
    }

    ~SdfBuilder()
    {
        OnDispose();
    }
}