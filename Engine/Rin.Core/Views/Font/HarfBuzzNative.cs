using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Rin.Core.Views.Font;

/// <summary>
///     Callback sink for <see cref="HarfBuzzNative.DrawGlyph" />, receiving a glyph outline in font units.
/// </summary>
public interface IHarfBuzzOutlineSink
{
    public void MoveTo(float x, float y);
    public void LineTo(float x, float y);
    public void QuadraticTo(float controlX, float controlY, float x, float y);
    public void CubicTo(float control1X, float control1Y, float control2X, float control2Y, float x, float y);
    public void ClosePath();
}

/// <summary>
///     Hand-written P/Invoke bindings against the raw HarfBuzz C API (hb.h / hb-ot.h), targeting the native
///     library shipped by the HarfBuzzSharp.NativeAssets.* packages. We do not depend on the HarfBuzzSharp
///     managed assembly - its wrapper omits the outline-extraction (hb_font_draw_glyph) and codepoint-coverage
///     (hb_face_collect_unicodes) APIs we need, so we bind the whole surface ourselves for a single consistent
///     access pattern.
/// </summary>
internal static partial class HarfBuzzNative
{
    private const string Lib = "libHarfBuzzSharp";

    // hb_memory_mode_t - real native order is DUPLICATE=0, READONLY=1, WRITABLE=2, READONLY_MAY_MAKE_WRITABLE=3.
    internal enum MemoryMode
    {
        Duplicate = 0,
        Readonly = 1,
        Writable = 2,
        ReadonlyMayMakeWritable = 3
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GlyphInfo
    {
        public uint Codepoint;
        private readonly uint _mask;
        public uint Cluster;
        private readonly uint _var1;
        private readonly uint _var2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GlyphPosition
    {
        public int XAdvance;
        public int YAdvance;
        public int XOffset;
        public int YOffset;
        private readonly uint _var;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FontExtents
    {
        public int Ascender;
        public int Descender;
        public int LineGap;
        private readonly int _r9, _r8, _r7, _r6, _r5, _r4, _r3, _r2, _r1;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GlyphExtents
    {
        public int XBearing;
        public int YBearing;
        public int Width;
        public int Height;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Feature
    {
        public uint Tag;
        public uint Value;
        public uint Start;
        public uint End;

        public static Feature Toggle(string fourCharTag, bool enabled)
        {
            return new Feature { Tag = MakeTag(fourCharTag), Value = enabled ? 1u : 0u, Start = 0, End = uint.MaxValue };
        }
    }

    internal static uint MakeTag(string fourChars)
    {
        return ((uint)fourChars[0] << 24) | ((uint)fourChars[1] << 16) | ((uint)fourChars[2] << 8) | fourChars[3];
    }

    #region Blob / Face / Font lifecycle

    [LibraryImport(Lib)]
    internal static unsafe partial IntPtr hb_blob_create(byte* data, uint length, MemoryMode mode, IntPtr userData, IntPtr destroy);

    [LibraryImport(Lib)]
    internal static partial void hb_blob_destroy(IntPtr blob);

    [LibraryImport(Lib)]
    internal static unsafe partial byte* hb_blob_get_data(IntPtr blob, out uint length);

    [LibraryImport(Lib)]
    internal static partial uint hb_blob_get_length(IntPtr blob);

    [LibraryImport(Lib)]
    internal static partial IntPtr hb_face_create(IntPtr blob, uint index);

    [LibraryImport(Lib)]
    internal static partial void hb_face_destroy(IntPtr face);

    [LibraryImport(Lib)]
    internal static partial uint hb_face_get_upem(IntPtr face);

    [LibraryImport(Lib)]
    internal static partial IntPtr hb_face_reference_table(IntPtr face, uint tag);

    internal static bool FaceHasTable(IntPtr face, string fourCharTag)
    {
        var blob = hb_face_reference_table(face, MakeTag(fourCharTag));
        var hasTable = hb_blob_get_length(blob) > 0;
        hb_blob_destroy(blob);
        return hasTable;
    }

    [LibraryImport(Lib)]
    internal static partial IntPtr hb_font_create(IntPtr face);

    [LibraryImport(Lib)]
    internal static partial void hb_font_destroy(IntPtr font);

    [LibraryImport(Lib)]
    internal static partial void hb_font_set_scale(IntPtr font, int xScale, int yScale);

    [LibraryImport(Lib)]
    internal static partial void hb_ot_font_set_funcs(IntPtr font);

    #endregion

    #region Metrics / glyph lookup

    [LibraryImport(Lib)]
    private static partial int hb_font_get_h_extents(IntPtr font, out FontExtents extents);

    internal static bool TryGetHorizontalFontExtents(IntPtr font, out FontExtents extents)
    {
        return hb_font_get_h_extents(font, out extents) != 0;
    }

    [LibraryImport(Lib)]
    private static partial int hb_font_get_nominal_glyph(IntPtr font, uint unicode, out uint glyph);

    internal static bool TryGetNominalGlyph(IntPtr font, uint unicode, out uint glyph)
    {
        return hb_font_get_nominal_glyph(font, unicode, out glyph) != 0;
    }

    [LibraryImport(Lib)]
    private static partial int hb_font_get_glyph_extents(IntPtr font, uint glyph, out GlyphExtents extents);

    internal static bool TryGetGlyphExtents(IntPtr font, uint glyph, out GlyphExtents extents)
    {
        return hb_font_get_glyph_extents(font, glyph, out extents) != 0;
    }

    #endregion

    #region Buffer / shaping

    [LibraryImport(Lib)]
    internal static partial IntPtr hb_buffer_create();

    [LibraryImport(Lib)]
    internal static partial void hb_buffer_destroy(IntPtr buffer);

    [LibraryImport(Lib)]
    internal static unsafe partial void hb_buffer_add_utf16(IntPtr buffer, char* text, int textLength, uint itemOffset, int itemLength);

    [LibraryImport(Lib)]
    internal static partial void hb_buffer_guess_segment_properties(IntPtr buffer);

    [LibraryImport(Lib)]
    internal static unsafe partial void hb_shape(IntPtr font, IntPtr buffer, Feature* features, uint numFeatures);

    [LibraryImport(Lib)]
    internal static unsafe partial GlyphInfo* hb_buffer_get_glyph_infos(IntPtr buffer, out uint length);

    [LibraryImport(Lib)]
    internal static unsafe partial GlyphPosition* hb_buffer_get_glyph_positions(IntPtr buffer, out uint length);

    #endregion

    #region Codepoint coverage

    [LibraryImport(Lib)]
    internal static partial IntPtr hb_set_create();

    [LibraryImport(Lib)]
    internal static partial void hb_set_destroy(IntPtr set);

    [LibraryImport(Lib)]
    internal static partial void hb_face_collect_unicodes(IntPtr face, IntPtr set);

    [LibraryImport(Lib)]
    internal static partial uint hb_set_get_population(IntPtr set);

    [LibraryImport(Lib)]
    internal static partial uint hb_set_next(IntPtr set, uint codepoint);

    private const uint SetInvalid = 0xFFFFFFFF; // HB_SET_VALUE_INVALID

    internal static IEnumerable<uint> EnumerateCodepoints(IntPtr face)
    {
        var set = hb_set_create();
        try
        {
            hb_face_collect_unicodes(face, set);
            var current = SetInvalid;
            while (true)
            {
                current = hb_set_next(set, current);
                if (current == SetInvalid) yield break;
                yield return current;
            }
        }
        finally
        {
            hb_set_destroy(set);
        }
    }

    #endregion

    #region OpenType name table

    [LibraryImport(Lib)]
    private static unsafe partial uint hb_ot_name_get_utf16(IntPtr face, uint nameId, IntPtr language, ref uint textSize, char* text);

    internal const uint NameIdFontFamily = 1;
    internal const uint NameIdTypographicFamily = 16;

    internal static unsafe string? GetName(IntPtr face, uint nameId)
    {
        const int capacity = 256;
        var buffer = new char[capacity];
        fixed (char* pBuffer = buffer)
        {
            var size = (uint)capacity;
            var written = hb_ot_name_get_utf16(face, nameId, IntPtr.Zero, ref size, pBuffer);
            if (written == 0) return null;
            var length = (int)Math.Min(size, capacity);
            return new string(buffer, 0, length);
        }
    }

    #endregion

    #region Draw funcs (glyph outline extraction)

    private static readonly IntPtr SharedDrawFuncs = CreateSharedDrawFuncs();

    [LibraryImport(Lib)]
    private static partial IntPtr hb_draw_funcs_create();

    [LibraryImport(Lib)]
    private static partial void hb_draw_funcs_set_move_to_func(IntPtr dfuncs, IntPtr func, IntPtr userData, IntPtr destroy);

    [LibraryImport(Lib)]
    private static partial void hb_draw_funcs_set_line_to_func(IntPtr dfuncs, IntPtr func, IntPtr userData, IntPtr destroy);

    [LibraryImport(Lib)]
    private static partial void hb_draw_funcs_set_quadratic_to_func(IntPtr dfuncs, IntPtr func, IntPtr userData, IntPtr destroy);

    [LibraryImport(Lib)]
    private static partial void hb_draw_funcs_set_cubic_to_func(IntPtr dfuncs, IntPtr func, IntPtr userData, IntPtr destroy);

    [LibraryImport(Lib)]
    private static partial void hb_draw_funcs_set_close_path_func(IntPtr dfuncs, IntPtr func, IntPtr userData, IntPtr destroy);

    [LibraryImport(Lib)]
    private static unsafe partial void hb_font_draw_glyph(IntPtr font, uint glyph, IntPtr dfuncs, void* drawData);

    private static unsafe IntPtr CreateSharedDrawFuncs()
    {
        var dfuncs = hb_draw_funcs_create();
        hb_draw_funcs_set_move_to_func(dfuncs, (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, void*, IntPtr, float, float, void*, void>)&OnMoveTo, IntPtr.Zero, IntPtr.Zero);
        hb_draw_funcs_set_line_to_func(dfuncs, (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, void*, IntPtr, float, float, void*, void>)&OnLineTo, IntPtr.Zero, IntPtr.Zero);
        hb_draw_funcs_set_quadratic_to_func(dfuncs, (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, void*, IntPtr, float, float, float, float, void*, void>)&OnQuadTo, IntPtr.Zero, IntPtr.Zero);
        hb_draw_funcs_set_cubic_to_func(dfuncs, (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, void*, IntPtr, float, float, float, float, float, float, void*, void>)&OnCubicTo, IntPtr.Zero, IntPtr.Zero);
        hb_draw_funcs_set_close_path_func(dfuncs, (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, void*, IntPtr, void*, void>)&OnClosePath, IntPtr.Zero, IntPtr.Zero);
        return dfuncs;
    }

    private static unsafe IHarfBuzzOutlineSink SinkFromDrawData(void* drawData)
    {
        var handle = GCHandle.FromIntPtr((IntPtr)drawData);
        return (IHarfBuzzOutlineSink)handle.Target!;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void OnMoveTo(IntPtr dfuncs, void* drawData, IntPtr state, float toX, float toY, void* userData)
    {
        SinkFromDrawData(drawData).MoveTo(toX, toY);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void OnLineTo(IntPtr dfuncs, void* drawData, IntPtr state, float toX, float toY, void* userData)
    {
        SinkFromDrawData(drawData).LineTo(toX, toY);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void OnQuadTo(IntPtr dfuncs, void* drawData, IntPtr state, float controlX, float controlY, float toX, float toY, void* userData)
    {
        SinkFromDrawData(drawData).QuadraticTo(controlX, controlY, toX, toY);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void OnCubicTo(IntPtr dfuncs, void* drawData, IntPtr state, float c1X, float c1Y, float c2X, float c2Y, float toX, float toY, void* userData)
    {
        SinkFromDrawData(drawData).CubicTo(c1X, c1Y, c2X, c2Y, toX, toY);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void OnClosePath(IntPtr dfuncs, void* drawData, IntPtr state, void* userData)
    {
        SinkFromDrawData(drawData).ClosePath();
    }

    /// <summary>
    ///     Extracts the outline of <paramref name="glyph" /> (in font units) into <paramref name="sink" />.
    /// </summary>
    internal static unsafe void DrawGlyph(IntPtr font, uint glyph, IHarfBuzzOutlineSink sink)
    {
        var handle = GCHandle.Alloc(sink, GCHandleType.Normal);
        try
        {
            hb_font_draw_glyph(font, glyph, SharedDrawFuncs, (void*)GCHandle.ToIntPtr(handle));
        }
        finally
        {
            handle.Free();
        }
    }

    #endregion
}
