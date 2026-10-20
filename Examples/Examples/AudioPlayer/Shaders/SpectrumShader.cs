using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Views.Graphics.Shaders;
using Rin.Shade;

namespace Examples.AudioPlayer.Shaders;

[NoReorder]
public struct SpectrumData
{
    public Matrix4x4 Projection;
    public Matrix4x4 Transform;
    public Vector2 Size;
    public float InnerRadius;
    public float OuterRadius;
    public float Pulse;
    public float Time;
    public VisualizerMode Mode;
    public int BandCount;
    public Vector4 PrimaryColor;
    public Vector4 SecondaryColor;
}

/// <summary>
///     The spectrum around the centre of its quad, in one of five looks picked by <see cref="VisualizerMode" />:
///     a ring of bars with falling peak caps, soft petals interpolated between bands, long rotating rays, orbiting
///     sparks over a faint ring, or straight bars under the cover. The radial ones follow the pulse at the inner
///     edge and reach past the outer radius, so the quad must be padded beyond it. All blend the cover's two colors.
///     Levels and peaks arrive packed four per float4: levels first, then peaks.
/// </summary>
[Shader]
public partial class SpectrumShader : ViewShader<SpectrumShader.FragmentIn>
{
    private const float Pi = 3.14159265f;
    private const float PulseReach = 8f;
    private const float LevelFloor = 0.2f;
    private const float LevelCurve = 1.4f;

    private const float GapFraction = 0.14f;
    private const float EdgeSoftness = 0.75f;
    private const float CapThickness = 2.5f;
    private const float CapGap = 5f;
    private const float GlowReach = 10f;
    private const float GlowPulseReach = 26f;
    private const float GlowStrength = 0.4f;

    private const float BarsWidth = 300f;
    private const float BarsHeight = 40f;
    private const float BarsGap = 3f;
    private const float BarsMinHeight = 3f;
    private const float BarsBottomInset = 2f;

    private const int BloomLayers = 3;
    private const float BloomSpin = 0.12f;
    private const float BloomSoftness = 10f;
    private const float BloomRimReach = 14f;
    private const float BloomBase = 0.22f;
    private const float BloomRim = 0.5f;

    private const float RaySpin = 0.05f;
    private const float RayLength = 1.9f;
    private const float RayWidth = 3f;
    private const float RayWidthLevel = 6f;
    private const float HaloReach = 18f;
    private const float HaloStrength = 0.35f;

    private const int SparkCount = 56;
    private const float SparkRingStrength = 0.3f;
    private const float SparkSize = 1.6f;
    private const float SparkSizeLevel = 3.2f;

    public struct PushConstants
    {
        public BufferRef<SpectrumData> Data;
        public BufferRef<Vector4> Bands;
    }

    public struct VertexIn
    {
        [VertexId] public int VertexId;
    }

    public struct VertexOut
    {
        [Semantic("UV")] public Vector2 Uv;
        [Position] public Vector4 Position;
    }

    public struct FragmentIn
    {
        [Semantic("UV")] public Vector2 Uv;
        [Position] public Vector2 Coordinate;
    }

    [Push] protected PushConstants Push;

    [Vertex]
    public VertexOut Vertex(VertexIn input)
    {
        var data = Push.Data[0];
        var corners = new[]
        {
            new Vector2(0f, 0f), new Vector2(data.Size.X, 0f), new Vector2(data.Size.X, data.Size.Y),
            new Vector2(0f, 0f), new Vector2(data.Size.X, data.Size.Y), new Vector2(0f, data.Size.Y)
        };

        var corner = corners[input.VertexId];
        var projected = Vector4.Transform(Vector4.Transform(new Vector4(corner, 0f, 1f), data.Transform), data.Projection);

        VertexOut output;
        output.Position = new Vector4(projected.X, projected.Y, 0f, 1f);
        output.Uv = corner / data.Size;
        return output;
    }

    private static float Lane(Vector4 value, int lane) => lane switch
    {
        0 => value.X,
        1 => value.Y,
        2 => value.Z,
        _ => value.W
    };

    private float BandValue(int index)
    {
        return Lane(Push.Bands[index / 4], index % 4);
    }

    // Music sits in the middle of the dB range, so the curve spreads 0.2 to 1 over the whole bar.
    private static float Shaped(float level)
    {
        return Pow(Saturate((level - LevelFloor) / (1f - LevelFloor)), LevelCurve);
    }

    // The level at a fractional band position, blended between the two nearest bands.
    private float SmoothLevel(float position, int bandCount)
    {
        var clamped = Clamp(position - 0.5f, 0f, bandCount - 1f);
        var first = (int)clamped;
        var second = first + 1 < bandCount ? first + 1 : first;
        var mix = clamped - first;
        mix = mix * mix * (3f - 2f * mix);
        return Lerp(Shaped(BandValue(first)), Shaped(BandValue(second)), mix);
    }

    private static float Hash(float seed)
    {
        return Frac(Sin(seed * 12.9898f + 78.233f) * 43758.5453f);
    }

    // 0 at the top, 1 at the bottom, the same on both sides, so a pattern over it is mirrored.
    private static float MirroredTurn(float angle)
    {
        var turn = Abs(angle + Pi / 2f);
        if (turn > Pi) turn = 2f * Pi - turn;
        return turn / Pi;
    }

    protected override Vector4 Color(FragmentIn input)
    {
        var data = Push.Data[0];
        var p = input.Uv * data.Size - data.Size / 2f;
        var radius = Length(p);
        var angle = Atan2(p.Y, p.X);
        var inner = data.InnerRadius + data.Pulse * PulseReach;
        var extent = data.OuterRadius - inner;

        if (data.Mode == VisualizerMode.Bloom) return Bloom(data, radius, angle, inner, extent);
        if (data.Mode == VisualizerMode.Rays) return Rays(data, radius, angle, inner, extent);
        if (data.Mode == VisualizerMode.Sparks) return Sparks(data, p, radius, angle, inner, extent);
        if (data.Mode == VisualizerMode.Bars) return Bars(data, p);
        return Ring(data, radius, angle, inner, extent, 1f);
    }

    private Vector4 Ring(SpectrumData data, float radius, float angle, float inner, float extent, float strength)
    {
        var turn = MirroredTurn(angle);
        var position = turn * data.BandCount;
        var band = (int)Min(position, data.BandCount - 1f);
        var within = Frac(position);
        var level = Shaped(BandValue(band));
        var peak = Shaped(BandValue(data.BandCount + band));
        var barOuter = inner + level * extent;

        // Signed distances in pixels, positive inside: along the arc the band is radius * (pi / bands) wide.
        var arcWidth = radius * Pi / data.BandCount;
        var angular = (0.5f - Abs(within - 0.5f) - GapFraction) * arcWidth;
        var bar = Min(Min(radius - inner, barOuter - radius), angular);
        var coverage = SmoothStep(-EdgeSoftness, EdgeSoftness, bar);

        var capCenter = inner + peak * extent + CapGap;
        var cap = Min(CapThickness / 2f - Abs(radius - capCenter), angular);
        var capCoverage = SmoothStep(-EdgeSoftness, EdgeSoftness, cap) * SmoothStep(0.02f, 0.08f, peak);

        var beyond = Max(radius - barOuter, 0f);
        var glowGate = SmoothStep(-EdgeSoftness, EdgeSoftness, angular);
        var glow = Exp(-beyond / (GlowReach + GlowPulseReach * data.Pulse)) * level * GlowStrength * glowGate;

        var tint = Lerp(data.PrimaryColor, data.SecondaryColor, turn).XYZ;
        var barColor = tint * (0.65f + 0.35f * level);
        var capColor = Lerp(tint, new Vector3(1f), 0.6f);

        var alpha = (Max(coverage, capCoverage) + glow * (1f - coverage)) * strength;
        var color = Lerp(barColor, capColor, capCoverage);
        return new Vector4(color, alpha);
    }

    // Straight rounded bars under the cover, sitting just inside the outer radius so the quad's padding is not used.
    private Vector4 Bars(SpectrumData data, Vector2 p)
    {
        var x = p.X + BarsWidth / 2f;
        if (x < 0f || x >= BarsWidth) return new Vector4(0f);

        var pitch = BarsWidth / data.BandCount;
        var band = (int)(x / pitch);
        var level = Shaped(BandValue(band));
        var width = pitch - BarsGap;
        var height = Max(BarsMinHeight, level * BarsHeight);
        var radius = width / 2f;

        var bottom = data.OuterRadius - BarsBottomInset;
        var center = new Vector2((band + 0.5f) * pitch, bottom - height / 2f);
        var half = new Vector2(width / 2f, height / 2f) - new Vector2(radius);
        var local = new Vector2(x, p.Y) - center;
        var outside = Max(Abs(local) - half, new Vector2(0f));
        var distance = Length(outside) - radius;
        var coverage = SmoothStep(EdgeSoftness, -EdgeSoftness, distance);

        var tint = Lerp(data.PrimaryColor, data.SecondaryColor, x / BarsWidth).XYZ;
        return new Vector4(tint, coverage * (0.3f + 0.7f * level));
    }

    // Three soft petal silhouettes, each the band curve spun a little differently, brightest at their edge.
    private Vector4 Bloom(SpectrumData data, float radius, float angle, float inner, float extent)
    {
        var color = new Vector3(0f);
        var alpha = 0f;
        for (var layer = 0; layer < BloomLayers; layer++)
        {
            var direction = layer % 2 == 0 ? 1f : -1f;
            var spin = data.Time * BloomSpin * direction * (layer + 1) + layer * 0.9f;
            var turn = MirroredTurn(angle + spin);
            var level = SmoothLevel(turn * data.BandCount, data.BandCount);
            var reach = inner + level * extent * (1.15f - 0.2f * layer);
            var edge = reach - radius;
            var coverage = SmoothStep(-BloomSoftness, 0f, edge) * SmoothStep(inner - 2f, inner + 6f, radius);
            var rim = Exp(-Max(edge, 0f) / BloomRimReach);
            var tint = Lerp(data.PrimaryColor, data.SecondaryColor, Saturate(0.25f * layer + 0.6f * level)).XYZ;
            var weight = coverage * (BloomBase + BloomRim * rim);
            color += tint * weight;
            alpha += weight;
        }

        return new Vector4(color / Max(alpha, 0.001f), Saturate(alpha));
    }

    // One ray per band, slowly turning, reaching past the outer radius on loud bands, with a halo on the beat.
    private Vector4 Rays(SpectrumData data, float radius, float angle, float inner, float extent)
    {
        var turn = Frac((angle + Pi) / (2f * Pi) + data.Time * RaySpin);
        var position = turn * data.BandCount;
        var band = (int)Min(position, data.BandCount - 1f);
        var within = Frac(position);
        var level = Shaped(BandValue(band));

        var length = extent * RayLength * level;
        var arcWidth = Max(radius, inner) * 2f * Pi / data.BandCount;
        var across = (within - 0.5f) * arcWidth;
        var sigma = RayWidth + RayWidthLevel * level;
        var profile = Exp(-across * across / (2f * sigma * sigma));

        var along = radius - inner;
        var fade = Exp(-Max(along, 0f) / Max(length, 1f)) * (1f - SmoothStep(length * 0.6f, length * 1.4f, along));
        var intensity = profile * fade * (0.35f + 0.65f * level);
        var halo = Exp(-Max(along, 0f) / (HaloReach * (0.4f + data.Pulse))) * data.Pulse * HaloStrength;

        var tint = Lerp(data.PrimaryColor, data.SecondaryColor, turn).XYZ;
        var color = Lerp(tint, new Vector3(1f), 0.25f * intensity);
        var alpha = Saturate(intensity + halo) * SmoothStep(inner - 8f, inner, radius);
        return new Vector4(color, alpha);
    }

    // Sparks orbit the disc at speeds set by their band's level, over a faint ring. No state: each spark's orbit and
    // phase come from a hash of its index, so the pattern is the same every run.
    private Vector4 Sparks(SpectrumData data, Vector2 p, float radius, float angle, float inner, float extent)
    {
        var ring = Ring(data, radius, angle, inner, extent, SparkRingStrength);
        var color = ring.XYZ * ring.W;
        var alpha = ring.W;
        for (var i = 0; i < SparkCount; i++)
        {
            var seed = (float)i;
            var level = Shaped(BandValue(i % data.BandCount));
            var orbit = inner + (0.15f + 0.95f * Hash(seed + 1f)) * extent;
            var speed = (0.25f + 0.75f * Hash(seed + 2f)) * (i % 2 == 0 ? 1f : -1f);
            var theta = Hash(seed + 3f) * 2f * Pi + data.Time * speed * (0.5f + 2f * level);
            var center = new Vector2(Cos(theta), Sin(theta)) * orbit;
            var distance = Length(p - center);
            var size = SparkSize + SparkSizeLevel * level;
            var glow = Exp(-distance * distance / (2f * size * size)) * (0.25f + level);
            var tint = Lerp(data.PrimaryColor, data.SecondaryColor, Hash(seed + 4f)).XYZ;
            color += Lerp(tint, new Vector3(1f), 0.5f * level) * glow;
            alpha += glow;
        }

        return new Vector4(color / Max(alpha, 0.001f), Saturate(alpha));
    }
}
