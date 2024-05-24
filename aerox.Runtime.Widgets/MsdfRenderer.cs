using System.Numerics;
using System.Runtime.InteropServices;
using aerox.Runtime.Math;
using SixLabors.Fonts;

namespace aerox.Runtime.Widgets;

public class MsdfRenderer : Disposable, IGlyphRenderer
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NativeGenerateDelegate(IntPtr data, uint width, uint height, uint byteSize);

    private IntPtr _context = IntPtr.Zero;

    public void BeginFigure()
    {
    }

    public void MoveTo(Vector2 point)
    {
        var a = ToNative(point);
        NativeMoveTo(_context, ref a);
    }

    public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
    {
        var a = ToNative(secondControlPoint);
        var b = ToNative(point);
        NativeQuadraticBezierTo(_context, ref a, ref b);
    }

    public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
    {
        var a = ToNative(secondControlPoint);
        var b = ToNative(thirdControlPoint);
        var c = ToNative(point);
        NativeCubicBezierTo(_context, ref a, ref b, ref c);
    }

    public void LineTo(Vector2 point)
    {
        var a = ToNative(point);
        NativeLineTo(_context, ref a);
    }

    public void EndFigure()
    {
    }

    public void EndGlyph()
    {
    }

    public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)
    {
        return true;
    }

    public void EndText()
    {
        NativeEnd(_context);
    }

    public void BeginText(in FontRectangle bounds)
    {
        if (_context != IntPtr.Zero) NativeFreeContext(_context);
        _context = NativeNewContext();
    }

    public TextDecorations EnabledDecorations()
    {
        return TextDecorations.None;
    }

    public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness)
    {
        throw new NotImplementedException();
    }


    [DllImport(Dlls.AeroxNative, EntryPoint = "msdfNewContext", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr NativeNewContext();

    [DllImport(Dlls.AeroxNative, EntryPoint = "msdfFreeContext", CallingConvention = CallingConvention.Cdecl)]
    private static extern void NativeFreeContext(IntPtr context);

    [DllImport(Dlls.AeroxNative, EntryPoint = "msdfGlyphContextMoveTo", CallingConvention = CallingConvention.Cdecl)]
    private static extern void NativeMoveTo(IntPtr context, ref Vector2<float> to);

    [DllImport(Dlls.AeroxNative, EntryPoint = "msdfGlyphContextQuadraticBezierTo",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern void NativeQuadraticBezierTo(IntPtr context, ref Vector2<float> control, ref Vector2<float> to);

    [DllImport(Dlls.AeroxNative, EntryPoint = "msdfGlyphContextCubicBezierTo",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern void NativeCubicBezierTo(IntPtr context, ref Vector2<float> control1, ref Vector2<float> control2,
        ref Vector2<float> to);

    [DllImport(Dlls.AeroxNative, EntryPoint = "msdfGlyphContextLineTo", CallingConvention = CallingConvention.Cdecl)]
    private static extern void NativeLineTo(IntPtr context, ref Vector2<float> to);

    [DllImport(Dlls.AeroxNative, EntryPoint = "msdfEndContext", CallingConvention = CallingConvention.Cdecl)]
    private static extern void NativeEnd(IntPtr context);

    [DllImport(Dlls.AeroxNative, EntryPoint = "msdfRenderContext", CallingConvention = CallingConvention.Cdecl)]
    private static extern void NativeRender(IntPtr context,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativeGenerateDelegate onRendered);

    // Renders into a 4 channel image
    public bool Generate(NativeGenerateDelegate callback)
    {
        if (_context == IntPtr.Zero) return false;
        NativeRender(_context, callback);
        return true;
    }

    private static Vector2<float> ToNative(Vector2 v)
    {
        return new Vector2<float>(v.X, v.Y);
    }

    protected override void OnDispose(bool isManual)
    {
        if (_context != IntPtr.Zero) NativeFreeContext(_context);
    }
}