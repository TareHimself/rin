using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Shared.Math;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Layouts;

namespace Rin.Core.Views.Composite;

/// <summary>
///     A scroll list of <see cref="ItemCount" /> fixed-size items that only keeps views for the visible ones, recycling them as it scrolls.
/// </summary>
public class VirtualListView : ScrollListView
{
    private const int Overscan = 2;
    private const int Unbound = -1;

    private readonly List<IView> _rows = [];
    private readonly List<int> _boundIndices = [];
    private readonly Func<IView> _createItem;
    private readonly Action<IView, int> _bindItem;
    private int _itemCount;
    private float _itemSize;

    public VirtualListView(Func<IView> createItem, Action<IView, int> bindItem, float itemSize)
    {
        _createItem = createItem;
        _bindItem = bindItem;
        _itemSize = itemSize;
    }

    [PublicAPI]
    public int ItemCount
    {
        get => _itemCount;
        set
        {
            if (value == _itemCount) return;
            _itemCount = int.Max(value, 0);
            ScrollTo(GetScroll());
            InvalidateDesiredSize();
        }
    }

    [PublicAPI]
    public float ItemSize
    {
        get => _itemSize;
        set
        {
            _itemSize = value;
            Refresh();
            InvalidateDesiredSize();
            InvalidateLayout();
        }
    }

    [PublicAPI] public int RowCount => _rows.Count;

    [PublicAPI]
    public bool IsAtEnd => GetScroll() >= GetMaxScroll() - 1f;

    /// <summary>
    ///     Rebinds every visible item, for when the data behind the indices changed.
    /// </summary>
    [PublicAPI]
    public void Refresh()
    {
        for (var i = 0; i < _boundIndices.Count; i++) _boundIndices[i] = Unbound;
    }

    [PublicAPI]
    public void ScrollToEnd()
    {
        ScrollTo(GetMaxScroll());
    }

    public override float GetMaxScroll()
    {
        return float.Max(_itemCount * _itemSize - GetAxisSize(), 0f);
    }

    public override void Update(float deltaTime)
    {
        SyncRows();
        base.Update(deltaTime);
    }

    public override void Collect(in Matrix4x4 transform, in Rect2D clip, CommandList commands)
    {
        SyncRows();
        base.Collect(transform, clip, commands);
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        var length = _itemCount * _itemSize;
        return Axis == Axis.Column ? new Vector2(0f, length) : new Vector2(length, 0f);
    }

    protected override Vector2 ArrangeContent(in Vector2 availableSpace)
    {
        var total = _itemCount * _itemSize;
        var size = Axis == Axis.Column
            ? new Vector2(availableSpace.X.FiniteOr(0f), availableSpace.Y.FiniteOr(total))
            : new Vector2(availableSpace.X.FiniteOr(total), availableSpace.Y.FiniteOr(0f));
        var rowSize = Axis == Axis.Column ? new Vector2(size.X, _itemSize) : new Vector2(_itemSize, size.Y);

        for (var i = 0; i < _rows.Count; i++)
            if (_boundIndices[i] != Unbound)
                _rows[i].Layout(rowSize);

        ScrollTo(GetScroll());
        return size;
    }

    private void SyncRows()
    {
        var firstIndex = 0;
        var endIndex = 0;
        var poolSize = 0;
        if (_itemCount > 0 && _itemSize > 0f)
        {
            var scroll = GetScroll();
            var axisSize = GetAxisSize();
            firstIndex = int.Max((int)(scroll / _itemSize) - Overscan, 0);
            endIndex = int.Min((int)MathF.Ceiling((scroll + axisSize) / _itemSize) + Overscan, _itemCount);
            poolSize = int.Min((int)MathF.Ceiling(axisSize / _itemSize) + 1 + 2 * Overscan, _itemCount);
        }

        if (poolSize > _rows.Count) GrowPool(poolSize);
        if (_rows.Count == 0) return;

        for (var index = firstIndex; index < endIndex; index++) BindRow(index % _rows.Count, index);
        for (var row = 0; row < _rows.Count; row++)
            if (!IsRowInWindow(row, firstIndex, endIndex))
                UnbindRow(row);
    }

    private void GrowPool(int count)
    {
        while (_rows.Count < count)
        {
            var row = _createItem();
            row.Visibility = Visibility.Collapsed;
            _rows.Add(row);
            _boundIndices.Add(Unbound);
            Add(row);
        }

        Refresh();
        InvalidateLayout();
    }

    private bool IsRowInWindow(int row, int firstIndex, int endIndex)
    {
        var count = _rows.Count;
        var index = firstIndex + ((row - firstIndex % count) + count) % count;
        return index < endIndex;
    }

    private void BindRow(int row, int index)
    {
        if (_boundIndices[row] == index) return;

        var view = _rows[row];
        var wasUnbound = _boundIndices[row] == Unbound;
        _boundIndices[row] = index;
        _bindItem(view, index);
        view.Offset = Axis == Axis.Column ? new Vector2(0f, index * _itemSize) : new Vector2(index * _itemSize, 0f);

        if (!wasUnbound) return;
        view.Visibility = Visibility.Visible;
        InvalidateLayout();
    }

    private void UnbindRow(int row)
    {
        _boundIndices[row] = Unbound;
        _rows[row].Visibility = Visibility.Collapsed;
    }
}
