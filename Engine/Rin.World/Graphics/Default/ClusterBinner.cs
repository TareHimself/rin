using System.Numerics;
using Rin.World.Graphics.Default.Shaders;

namespace Rin.World.Graphics.Default;

/// <summary>
///     Sorts one view's bounded items (anything with a sphere of influence) by kind and then by view depth, and fills
///     the z-bins: for each kind and depth bin, the first and last sorted index of the items of that kind that
///     overlap it. A shader narrows a tile's item bitmask to that range.
/// </summary>
/// <remarks>
///     Bins are exponential in view depth between the near plane and the camera's far distance. A bin packs its first
///     index in the low 16 bits and its last in the high 16 bits, and an empty bin has first greater than last.
///     Arrays grow with the item count and are reused, so binning the same number of items again does not allocate.
/// </remarks>
public sealed class ClusterBinner
{
    public const int BinCount = 1024;
    public const int MaxItems = ushort.MaxValue;
    public const uint EmptyBin = ushort.MaxValue;

    public static readonly int TypeCount = Enum.GetValues<ClusterItemType>().Length;

    private readonly uint[] _bins = new uint[TypeCount * BinCount];
    private readonly int[] _typeStarts = new int[TypeCount + 1];
    private ulong[] _sortKeys = [];
    private int[] _order = [];
    private ClusterItem[] _added = [];
    private float[] _addedDepths = [];
    private ClusterItem[] _items = [];
    private float[] _depths = [];
    private Matrix4x4 _view;
    private float _near;
    private float _far;
    private int _addedCount;
    private int _count;

    /// <summary>
    ///     Every kept item: grouped by kind in <see cref="ClusterItemType" /> order, nearest first within a kind.
    /// </summary>
    public ReadOnlySpan<ClusterItem> Items => _items.AsSpan(0, _count);

    /// <summary>
    ///     <see cref="BinCount" /> bins per kind, one kind after another.
    /// </summary>
    public ReadOnlySpan<uint> Bins => _bins;

    public ReadOnlySpan<uint> BinsOf(ClusterItemType type)
    {
        return _bins.AsSpan((int)type * BinCount, BinCount);
    }

    public int StartOf(ClusterItemType type)
    {
        return _typeStarts[(int)type];
    }

    public int CountOf(ClusterItemType type)
    {
        return _typeStarts[(int)type + 1] - _typeStarts[(int)type];
    }

    public void Begin(in Matrix4x4 view, float near, float far)
    {
        _view = view;
        _near = near;
        _far = far;
        _addedCount = 0;
        _count = 0;
    }

    /// <summary>
    ///     Adds a sphere, unless it lies entirely nearer than the near plane or farther than the far distance.
    ///     <paramref name="index" /> is the item's position in its kind's data array.
    /// </summary>
    public void Add(ClusterItemType type, uint index, Vector3 center, float radius)
    {
        var depth = Vector3.Transform(center, _view).Z;
        if (depth + radius < _near || depth - radius > _far) return;

        EnsureCapacity(_addedCount + 1);
        _sortKeys[_addedCount] = SortKey(type, depth);
        _order[_addedCount] = _addedCount;
        _addedDepths[_addedCount] = depth;
        _added[_addedCount] = new ClusterItem { Center = center, Radius = radius, Type = type, Index = index };
        _addedCount++;
    }

    /// <summary>
    ///     Sorts what was added and fills the bins. Past <see cref="MaxItems" /> the last items are dropped.
    /// </summary>
    public void Build()
    {
        _sortKeys.AsSpan(0, _addedCount).Sort(_order.AsSpan(0, _addedCount));
        _count = int.Min(_addedCount, MaxItems);

        Array.Clear(_typeStarts);
        for (var i = 0; i < _count; i++)
        {
            _items[i] = _added[_order[i]];
            _depths[i] = _addedDepths[_order[i]];
            _typeStarts[(int)_items[i].Type + 1]++;
        }

        for (var type = 0; type < TypeCount; type++) _typeStarts[type + 1] += _typeStarts[type];

        Array.Fill(_bins, EmptyBin);
        for (var i = 0; i < _count; i++)
        {
            var bins = _bins.AsSpan((int)_items[i].Type * BinCount, BinCount);
            var first = BinOf(_depths[i] - _items[i].Radius, _near, _far);
            var last = BinOf(_depths[i] + _items[i].Radius, _near, _far);
            for (var bin = first; bin <= last; bin++) bins[bin] = Include(bins[bin], (uint)i);
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
    ///     The first and last sorted item index a bin covers, or false for an empty bin.
    /// </summary>
    public static bool TryGetRange(uint bin, out int first, out int last)
    {
        first = (int)(bin & 0xFFFF);
        last = (int)(bin >> 16);
        return first <= last;
    }

    // Items are added in ascending index order, so the first index set stays the minimum.
    private static uint Include(uint bin, uint index)
    {
        var first = bin == EmptyBin ? index : bin & 0xFFFF;
        return first | (index << 16);
    }

    // Kind in the high half, and the depth's bits flipped so that they order the same way as the floats they encode.
    private static ulong SortKey(ClusterItemType type, float depth)
    {
        var bits = BitConverter.SingleToUInt32Bits(depth);
        var ordered = (bits & 0x80000000u) != 0 ? ~bits : bits | 0x80000000u;
        return ((ulong)type << 32) | ordered;
    }

    private void EnsureCapacity(int count)
    {
        if (_added.Length >= count) return;

        var capacity = int.Max(count, _added.Length * 2);
        Array.Resize(ref _sortKeys, capacity);
        Array.Resize(ref _order, capacity);
        Array.Resize(ref _added, capacity);
        Array.Resize(ref _addedDepths, capacity);
        _items = new ClusterItem[capacity];
        _depths = new float[capacity];
    }
}
