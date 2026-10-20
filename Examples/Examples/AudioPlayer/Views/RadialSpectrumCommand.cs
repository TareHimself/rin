using System.Numerics;
using Examples.AudioPlayer.Library;
using Examples.AudioPlayer.Shaders;
using Rin.Core.Views.Graphics.Commands;
using Rin.Core.Views.Graphics.PassConfigs;

namespace Examples.AudioPlayer.Views;

public sealed class RadialSpectrumCommand(
    Matrix4x4 transform,
    Vector2 size,
    Vector4[] packedBands,
    int bandCount,
    float innerRadius,
    float outerRadius,
    float pulse,
    CoverPalette palette,
    VisualizerMode mode)
    : TCommand<MainPassConfig, RadialSpectrumCommandHandler>
{
    public Matrix4x4 Transform => transform;

    public Vector2 Size => size;

    /// <summary>
    ///     The band levels followed by the band peaks, four per entry.
    /// </summary>
    public Vector4[] PackedBands => packedBands;

    public int BandCount => bandCount;

    public float InnerRadius => innerRadius;

    public float OuterRadius => outerRadius;

    public float Pulse => pulse;

    public CoverPalette Palette => palette;

    public VisualizerMode Mode => mode;
}
