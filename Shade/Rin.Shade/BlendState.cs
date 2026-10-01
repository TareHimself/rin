namespace Rin.Shade;

/// <summary>
/// Multiplier applied to the source or destination term of a blend equation.
/// </summary>
public enum BlendFactor
{
    Zero,
    One,
    SrcColor,
    OneMinusSrcColor,
    DstColor,
    OneMinusDstColor,
    SrcAlpha,
    OneMinusSrcAlpha,
    DstAlpha,
    OneMinusDstAlpha
}

/// <summary>
/// How the weighted source and destination terms are combined.
/// </summary>
public enum BlendOp
{
    Add,
    Subtract,
    ReverseSubtract,
    Min,
    Max
}

/// <summary>
/// A pair of blend equations (color and alpha) plus a color write toggle.
/// </summary>
/// <remarks>
/// <see cref="None" /> and <see cref="Opaque" /> have the same equation and differ only in
/// <see cref="WritesColor" />, which is why it is an explicit field and not inferred from a preset.
/// </remarks>
public readonly record struct BlendState(
    BlendFactor SrcColor, BlendFactor DstColor, BlendOp ColorOp,
    BlendFactor SrcAlpha, BlendFactor DstAlpha, BlendOp AlphaOp,
    bool WritesColor = true)
{
    /// <summary>
    /// Writes no color at all, for passes such as stencil-only that still declare a fragment output.
    /// </summary>
    public static readonly BlendState None =
        new(BlendFactor.One, BlendFactor.Zero, BlendOp.Add, BlendFactor.One, BlendFactor.Zero, BlendOp.Add,
            WritesColor: false);

    /// <summary>
    /// Standard alpha blending: <c>src * srcAlpha + dst * (1 - srcAlpha)</c>.
    /// </summary>
    public static readonly BlendState Alpha =
        new(BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha, BlendOp.Add,
            BlendFactor.One, BlendFactor.OneMinusSrcAlpha, BlendOp.Add);

    /// <summary>
    /// Adds source to destination, so it only ever brightens.
    /// </summary>
    public static readonly BlendState Additive =
        new(BlendFactor.One, BlendFactor.One, BlendOp.Add, BlendFactor.One, BlendFactor.One, BlendOp.Add);

    /// <summary>
    /// Overwrites the destination with the source, with blending disabled.
    /// </summary>
    public static readonly BlendState Opaque =
        new(BlendFactor.One, BlendFactor.Zero, BlendOp.Add, BlendFactor.One, BlendFactor.Zero, BlendOp.Add);
}
