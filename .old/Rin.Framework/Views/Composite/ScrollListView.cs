using System.Numerics;
using JetBrains.Annotations;
using Rin.Framework.Graphics;
using Rin.Framework.Shared.Math;
using Rin.Framework.Views.Graphics.Quads;
using Rin.Framework.Views.Events;
using Rin.Framework.Views.Graphics;
using Rin.Framework.Views.Layouts;

namespace Rin.Framework.Views.Composite;

/// <summary>
///     Slot = <see cref="Slot" />
/// </summary>
public class ScrollListView : ListView
{
    private CursorDownSurfaceEvent? _lastDownEvent;
    private float _maxOffset;
    private float _mouseDownOffset;
    private Vector2 _mouseDownPos;
    private float _offset;

    [PublicAPI]
    public float BarMinimumSize { get; set; } = 40.0f;
    [PublicAPI]
    public float BarWidth { get; set; } = 8.0f;

    [PublicAPI]
    public float BarPadding { get; set; } = 2.0f;
    
    [PublicAPI]
    public Color BarColor { get; set; } = Color.White;
    
    [PublicAPI]
    public bool FloatingBar { get; set; } = true;

    public ScrollListView()
    {
        Clip = Clip.Bounds;
    }

    public float ScrollScale { get; set; } = 10.0f;

    public virtual bool ScrollBy(float delta)
    {
        var finalOffset = _offset + delta;
        return ScrollTo(finalOffset);
    }

    public virtual bool ScrollTo(float offset)
    {
        var scrollSize = GetMaxScroll();

        if (scrollSize < 0) return false;

        _offset = float.Clamp(offset, 0, scrollSize);

        return float.Abs(offset - _offset) > 0.001;
    }

    protected override Vector2 ArrangeContent(in Vector2 spaceGiven)
    {
        var spaceTaken = base.ArrangeContent(spaceGiven);
        _maxOffset = Axis switch
        {
            Axis.Column => float.Max(spaceTaken.Y - spaceGiven.Y.FiniteOr(spaceTaken.Y), 0),
            Axis.Row => float.Max(spaceTaken.X - spaceGiven.X.FiniteOr(spaceTaken.X), 0),
            _ => throw new ArgumentOutOfRangeException()
        };
        ScrollTo(_offset);
        return new Vector2(float.Min(spaceTaken.X, spaceGiven.X), float.Min(spaceTaken.Y, spaceGiven.Y));
    }
    
    protected float GetBarCrossAxisSpaceTaken() => BarWidth + (BarPadding * 2);

    public virtual float GetScroll()
    {
        return _offset;
    }

    public virtual float GetAxisSize()
    {
        var size = GetContentSize();
        return Axis switch
        {
            Axis.Row => size.X,
            Axis.Column => size.Y,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public virtual float GetDesiredAxisSize()
    {
        var desiredSize = GetDesiredContentSize();

        return Axis switch
        {
            Axis.Row => desiredSize.X,
            Axis.Column => desiredSize.Y,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public virtual float GetMaxScroll()
    {
        return _maxOffset;
    }

    public virtual bool IsScrollable()
    {
        return GetMaxScroll() > 0;
    }

    protected override bool OnScroll(ScrollSurfaceEvent e)
    {
        if (!IsScrollable()) return base.OnScroll(e);

        switch (Axis)
        {
            case Axis.Column:
            {
                var delta = e.Delta.Y * -1.0f;
                return ScrollBy(delta * ScrollScale);
            }
            case Axis.Row:
            {
                var delta = e.Delta.X;
                return ScrollBy(delta * ScrollScale);
            }
            default:
                return base.OnScroll(e);
        }
    }

    protected virtual void CollectBarContent(in Matrix4x4 barTransform, in Vector2 barSize, CommandList commands)
    {
        commands.AddRect(barTransform,barSize,BarColor, new Vector4(7f));
    }

    protected virtual void CollectBar(in Matrix4x4 transform, in Rect2D clip, CommandList commands)
    {
        if (IsVisible && IsScrollable())
        {
            var scroll = GetScroll();
            var maxScroll = GetMaxScroll();
            var axisSize = GetAxisSize();
            var desiredAxisSize = axisSize + maxScroll;

            var barSize = float.Max(BarMinimumSize, axisSize - (desiredAxisSize - axisSize));
            var barCrossAxisOffset = GetBarCrossAxisSpaceTaken() - BarPadding;
            var availableDist = axisSize - barSize;
            var drawOffset = availableDist * (float.Max(scroll, 0.0001f) / maxScroll);

            var size = GetContentSize();
            
            switch (Axis)
            {
                case Axis.Column:
                {
                    var barTransform = transform.Translate(new Vector2(size.X - barCrossAxisOffset, drawOffset));
                    CollectBarContent(barTransform, new Vector2(BarWidth, barSize), commands);
                }
                    break;
                case Axis.Row:
                {
                    var barTransform = transform.Translate(new Vector2(drawOffset, size.Y - barCrossAxisOffset));
                    CollectBarContent(barTransform, new Vector2(barSize, BarWidth), commands);
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        var computedSize = base.LayoutContent(in availableSpace);
        
        if (FloatingBar)
        {
            return computedSize;
        }

        var barCrossAxisSize = GetBarCrossAxisSpaceTaken();
        
        // We recompute the size if the available space is finite and the computed size plus the bar cross axis size is greater than the available space
        switch (Axis)
        {
            case Axis.Column:
            {
                computedSize = float.IsFinite(availableSpace.X) && computedSize.X + barCrossAxisSize > availableSpace.X ? base.LayoutContent(availableSpace with{ X = availableSpace.X  - barCrossAxisSize }) : computedSize;

                computedSize.X += barCrossAxisSize;
                
                return computedSize;
            }
            case Axis.Row:
            {
                computedSize = float.IsFinite(availableSpace.Y) && computedSize.Y + barCrossAxisSize > availableSpace.Y ? base.LayoutContent(availableSpace with{ Y = availableSpace.Y  - barCrossAxisSize }) : computedSize;

                computedSize.Y += barCrossAxisSize;
                
                return computedSize;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override void Collect(in Matrix4x4 transform, in Rect2D clip, CommandList commands)
    {
        base.Collect(transform, clip, commands);
        CollectBar(transform, clip, commands);
    }
    
    public override Matrix4x4 GetLocalContentTransform()
    {
        return base.GetLocalContentTransform().ApplyBefore(Matrix4x4.Identity.Translate(Axis switch
        {
            Axis.Row => new Vector2(-GetScroll(), 0.0f),
            Axis.Column => new Vector2(0.0f, -GetScroll()),
            _ => throw new ArgumentOutOfRangeException(nameof(Axis))
        }));
    }

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        _mouseDownOffset = _offset;
        _mouseDownPos = e.Position;
        _lastDownEvent = e;
        e.Target = this;
    }

    public override void OnCursorMove(CursorMoveSurfaceEvent e, in Matrix4x4 transform)
    {
        if (_lastDownEvent?.Target == this)
        {
            var pos = _mouseDownPos - e.Position;

            if (ScrollTo(Axis switch
                {
                    Axis.Row => _mouseDownOffset + pos.X,
                    Axis.Column => _mouseDownOffset + pos.Y,
                    _ => throw new ArgumentOutOfRangeException()
                }))
            {
                e.Target = this;
            }
        }
    }

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        base.OnCursorUp(e);
        _lastDownEvent = null;
    }
}