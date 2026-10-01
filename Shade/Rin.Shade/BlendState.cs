namespace Rin.Shade;

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

public enum BlendOp
{
    Add,
    Subtract,
    ReverseSubtract,
    Min,
    Max
}

/// <summary>
///     Replaces the old closed <c>BlendMode</c> enum - blend mode is really a pair of blend
///     equations (color, alpha), and a fixed set of named cases can't express a mode that isn't
///     already one of them. <see cref="None" /> and <see cref="Opaque" /> share the same (no-op)
///     equation - the only difference between them is <see cref="WritesColor" />, a real field
///     rather than something inferred by comparing against a named preset: this is a record
///     struct, so distinguishing them by value equality would never work - they're equal by value
///     on purpose.
/// </summary>
public readonly record struct BlendState(
    BlendFactor SrcColor, BlendFactor DstColor, BlendOp ColorOp,
    BlendFactor SrcAlpha, BlendFactor DstAlpha, BlendOp AlphaOp,
    bool WritesColor = true)
{
    /// <summary>
    ///     No color attachment write at all (used by e.g. a stencil-only pass that still needs a
    ///     fragment output declared). The equation itself is the same no-op as <see cref="Opaque" />.
    /// </summary>
    public static readonly BlendState None =
        new(BlendFactor.One, BlendFactor.Zero, BlendOp.Add, BlendFactor.One, BlendFactor.Zero, BlendOp.Add,
            WritesColor: false);

    /// <summary>result = src*srcAlpha + dst*(1-srcAlpha) - attenuates the destination, can occlude.</summary>
    public static readonly BlendState Alpha =
        new(BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha, BlendOp.Add,
            BlendFactor.One, BlendFactor.OneMinusSrcAlpha, BlendOp.Add);

    /// <summary>result = src + dst - never attenuates the destination, only brightens.</summary>
    public static readonly BlendState Additive =
        new(BlendFactor.One, BlendFactor.One, BlendOp.Add, BlendFactor.One, BlendFactor.One, BlendOp.Add);

    /// <summary>Straight overwrite, full write mask - blending disabled.</summary>
    public static readonly BlendState Opaque =
        new(BlendFactor.One, BlendFactor.Zero, BlendOp.Add, BlendFactor.One, BlendFactor.Zero, BlendOp.Add);
}
