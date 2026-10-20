using System.Numerics;
using Examples.AudioPlayer.Library;
using Examples.AudioPlayer.Playback;
using Examples.AudioPlayer.Shaders;
using Rin.Core.Shared.Math;
using Rin.Core.Views;
using Rin.Core.Views.Graphics;

namespace Examples.AudioPlayer.Views;

/// <summary>
///     A square that draws the analyzer's bands as a ring through <see cref="RadialSpectrumCommand" />, colored from
///     the palette the callback returns, which it eases towards so a track change fades rather than snaps. The quad
///     it draws extends <see cref="Overflow" /> past its own bounds on every side, so rays, glow and sparks that reach
///     beyond the outer radius fade out on their own instead of being cut off at the edge.
/// </summary>
public sealed class RadialSpectrumView(SpectrumAnalyzer analyzer, Func<CoverPalette> palette) : ContentView
{
    private const float DefaultSide = 360f;
    private const float PaletteEasePerSecond = 3f;
    private const int LanesPerEntry = 4;

    private readonly Vector4[] _packed = new Vector4[2 * SpectrumAnalyzer.BandCount / LanesPerEntry];
    private CoverPalette _shown = palette();

    public float InnerRadius { get; init; } = 138f;

    public float OuterRadius { get; init; } = 178f;

    public VisualizerMode Mode { get; set; } = VisualizerMode.Ring;

    public float Overflow { get; init; } = 100f;

    public override Vector2 ComputeDesiredContentSize()
    {
        return new Vector2(DefaultSide);
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return new Vector2(float.IsFinite(availableSpace.X) ? availableSpace.X : DefaultSide);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        var target = palette();
        var ease = 1f - MathF.Exp(-deltaTime * PaletteEasePerSecond);
        _shown = new CoverPalette(Ease(_shown.Primary, target.Primary, ease), Ease(_shown.Secondary, target.Secondary, ease));
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        Pack(analyzer.Levels, 0);
        Pack(analyzer.Peaks, SpectrumAnalyzer.BandCount);
        var quad = GetContentSize() + new Vector2(2f * Overflow);
        commands.Add(new RadialSpectrumCommand(transform.Translate(new Vector2(-Overflow)), quad, _packed,
            SpectrumAnalyzer.BandCount, InnerRadius, OuterRadius, analyzer.Pulse, _shown, Mode));
    }

    private void Pack(ReadOnlySpan<float> values, int firstLane)
    {
        for (var i = 0; i < values.Length; i++)
        {
            var lane = firstLane + i;
            ref var entry = ref _packed[lane / LanesPerEntry];
            switch (lane % LanesPerEntry)
            {
                case 0: entry.X = values[i]; break;
                case 1: entry.Y = values[i]; break;
                case 2: entry.Z = values[i]; break;
                default: entry.W = values[i]; break;
            }
        }
    }

    private static Color Ease(Color from, Color to, float amount)
    {
        return new Color(
            from.R + (to.R - from.R) * amount,
            from.G + (to.G - from.G) * amount,
            from.B + (to.B - from.B) * amount,
            from.A + (to.A - from.A) * amount);
    }
}
