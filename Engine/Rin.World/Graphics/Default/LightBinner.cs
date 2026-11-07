using System.Numerics;

namespace Rin.World.Graphics.Default;

/// <summary>
///     Sorts one view's point lights by view depth and fills the z-bins: for each depth bin, the first and last index (in
///     sorted order) of the lights that overlap it. The forward shader narrows a tile's light bitmask to that range.
///     Directional lights are not binned, every fragment loops them.
/// </summary>
/// <remarks>
///     Bins are exponential in view depth between the near plane and the camera's far distance. A bin packs its first
///     index in the low 16 bits and its last in the high 16 bits, and an empty bin has first greater than last.
///     Arrays grow with the light count and are reused, so binning the same number of lights again does not allocate.
/// </remarks>
public sealed class LightBinner
{
    public const int BinCount = 1024;
    public const int MaxPointLights = ushort.MaxValue;
    public const uint EmptyBin = ushort.MaxValue;

    private readonly uint[] _bins = new uint[BinCount];
    private float[] _depths = [];
    private LightInfo[] _pointLights = [];
    private LightInfo[] _directionalLights = [];
    private int _pointCount;
    private int _directionalCount;

    public ReadOnlySpan<uint> Bins => _bins;

    /// <summary>
    ///     The binned point lights, nearest first.
    /// </summary>
    public ReadOnlySpan<LightInfo> PointLights => _pointLights.AsSpan(0, _pointCount);

    public ReadOnlySpan<LightInfo> DirectionalLights => _directionalLights.AsSpan(0, _directionalCount);

    /// <summary>
    ///     Point lights entirely nearer than <paramref name="near" /> or farther than <paramref name="far" /> are dropped,
    ///     and past <see cref="MaxPointLights" /> the farthest are dropped.
    /// </summary>
    public void Bin(ReadOnlySpan<LightInfo> lights, in Matrix4x4 view, float near, float far)
    {
        EnsureCapacity(lights.Length);
        _pointCount = 0;
        _directionalCount = 0;

        foreach (ref readonly var light in lights)
        {
            if (light.LightType == LightType.Directional)
            {
                _directionalLights[_directionalCount++] = light;
                continue;
            }

            var depth = Vector3.Transform(light.Location, view).Z;
            if (depth + light.Radius < near || depth - light.Radius > far) continue;

            _depths[_pointCount] = depth;
            _pointLights[_pointCount] = light;
            _pointCount++;
        }

        _depths.AsSpan(0, _pointCount).Sort(_pointLights.AsSpan(0, _pointCount));
        _pointCount = int.Min(_pointCount, MaxPointLights);

        Array.Fill(_bins, EmptyBin);
        for (var i = 0; i < _pointCount; i++)
        {
            var radius = _pointLights[i].Radius;
            var first = BinOf(_depths[i] - radius, near, far);
            var last = BinOf(_depths[i] + radius, near, far);
            for (var bin = first; bin <= last; bin++) _bins[bin] = Include(_bins[bin], (uint)i);
        }
    }

    /// <summary>
    ///     The bin a view depth falls in. Depths before <paramref name="near" /> map to the first bin and depths past
    ///     <paramref name="far" /> to the last.
    /// </summary>
    public static int BinOf(float depth, float near, float far)
    {
        if (depth <= near) return 0;
        var bin = (int)(float.Log(depth / near) * BinCount / float.Log(far / near));
        return int.Clamp(bin, 0, BinCount - 1);
    }

    /// <summary>
    ///     The first and last sorted light index a bin covers, or false for an empty bin.
    /// </summary>
    public static bool TryGetRange(uint bin, out int first, out int last)
    {
        first = (int)(bin & 0xFFFF);
        last = (int)(bin >> 16);
        return first <= last;
    }

    // Lights are added in ascending index order, so the first index set stays the minimum.
    private static uint Include(uint bin, uint index)
    {
        var first = bin == EmptyBin ? index : bin & 0xFFFF;
        return first | (index << 16);
    }

    private void EnsureCapacity(int count)
    {
        if (_depths.Length >= count) return;
        _depths = new float[count];
        _pointLights = new LightInfo[count];
        _directionalLights = new LightInfo[count];
    }
}
