using System.Numerics;
using rin.Framework.Core;
using rin.Framework.Core.Math;
using rin.Sdf;

namespace rin.Framework.Views.Sdf;

/// <summary>
/// Generates a MSDF/MTSDF using <a href="https://github.com/Chlumsky/msdfgen">msdfgen</a>
/// </summary>
public partial class Context : IDisposable
{
    private IntPtr _context = Native.Sdf.ContextNew();
    
    public Context MoveTo(Vector2 point)
    {
        Native.Sdf.ContextMoveTo(_context, ref point);
        return this;
    }

    public Context QuadraticBezierTo(Vector2 control, Vector2 point)
    {
        Native.Sdf.ContextQuadraticBezierTo(_context, ref control, ref point);
        return this;
    }
    
    
    public Context CubicBezierTo(Vector2 control1,
        Vector2 control2,
        Vector2 point)
    {
        Native.Sdf.ContextCubicBezierTo(_context,ref control1,ref control2,ref point);
        return this;
    }
    
    public Context LineTo(Vector2 point)
    {
        Native.Sdf.ContextLineTo(_context,ref point);
        return this;
    }
    
    /// <summary>
    /// Stop drawing the vector
    /// </summary>
    /// <returns></returns>
    public Context End()
    {
        Native.Sdf.ContextEnd(_context);
        return this;
    }
    
    public Result? GenerateMsdf(float angleThreshold,float pixelRange)
    {
        Result? result = null;
        
        unsafe
        {
            Native.Sdf.ContextGenerateMsdf(_context,angleThreshold,pixelRange, (data, pixelWidth, pixelHeight, count, width, height) =>
            {
                var buffer = new NativeBuffer<byte>((int)count);
                buffer.Write(data,count);
                result = new Result(buffer)
                {
                    PixelWidth = (int)pixelWidth,
                    PixelHeight = (int)pixelHeight,
                    Width = width,
                    Height = height,
                    Channels = 3,
                };
            });
        }
        
        return result;
    }

    public Result? GenerateMtsdf(float angleThreshold,float pixelRange)
    {
        Result? result = null;

        unsafe
        {
            Native.Sdf.ContextGenerateMtsdf(_context,angleThreshold,pixelRange, (data, pixelWidth, pixelHeight, count, width, height) =>
            {
                var buffer = new NativeBuffer<byte>((int)count);
                buffer.Write(data,count);
                result = new Result(buffer)
                {
                    PixelWidth = (int)pixelWidth,
                    PixelHeight = (int)pixelHeight,
                    Width = width,
                    Height = height,
                    Channels = 4,
                };
            });
        }

        return result;
    }
    
    private void OnDispose()
    {
        if(_context != IntPtr.Zero) Native.Sdf.ContextFree(_context);
        _context = IntPtr.Zero;
    }

    public void Dispose()
    {
        OnDispose();
        GC.SuppressFinalize(this);
    }

    ~Context()
    {
        OnDispose();
    }
}