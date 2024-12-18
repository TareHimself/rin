using rin.Framework.Core.Math;
using rin.Framework.Views.Events;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Graphics.Quads;

namespace rin.Framework.Views.Composite;

/// <summary>
///     Slot = <see cref="CompositeViewSlot" />
/// </summary>
public class ScrollList : List
{
    private float _mouseDownOffset;
    private Vector2<float> _mouseDownPos = new(0.0f);
    private float _offset;
    private float _maxOffset;
    private CursorDownEvent? _lastDownEvent;

    public float MinBarSize = 40.0f;

    public ScrollList()
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

        _offset = Math.Clamp(offset, 0, scrollSize);

        return Math.Abs(offset - _offset) > 0.001;
    }

    protected override Vector2<float> ArrangeContent(Vector2<float> spaceGiven)
    {
        var spaceTaken = base.ArrangeContent(spaceGiven);
        
        
        _maxOffset = Axis switch
        {
            Axis.Column => Math.Max(spaceTaken.Y - spaceGiven.Y.FiniteOr(spaceTaken.Y),0),
            Axis.Row => Math.Max(spaceTaken.X - spaceGiven.X.FiniteOr(spaceTaken.X),0),
            _ => throw new ArgumentOutOfRangeException()
        };
        ScrollTo(_offset);
        return new Vector2<float>(Math.Min(spaceTaken.X,spaceGiven.X),Math.Min(spaceTaken.Y,spaceGiven.Y));
    }

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

    public virtual float GetMaxScroll() => _maxOffset;

    public virtual bool IsScrollable()
    {
        return GetMaxScroll() > 0;
    }

    protected override bool OnScroll(ScrollEvent e)
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

    public override void Collect(Matrix3 transform, Views.Rect clip, DrawCommands drawCommands)
    {
        base.Collect(transform,clip, drawCommands);
        if (IsVisible && IsScrollable())
        {
            var scroll = GetScroll();
            var maxScroll = GetMaxScroll();
            var axisSize = GetAxisSize();
            var desiredAxisSize = axisSize + maxScroll;

            var barSize = Math.Max(MinBarSize, axisSize - (desiredAxisSize - axisSize));
            var availableDist = axisSize - barSize;
            var drawOffset = (float)(availableDist * (Math.Max(scroll, 0.0001) / maxScroll));

            var size = GetContentSize();

            var barTransform = transform.Translate(new Vector2<float>(size.X - 10.0f, drawOffset));
            drawCommands.AddRect(barTransform, new Vector2<float>(10.0f, barSize), color: Color.White, borderRadius: 7.0f);
        }
    }

    protected override Matrix3 ComputeSlotTransform(CompositeViewSlot slot, Matrix3 contentTransform)
    {
        return contentTransform.Translate(Axis switch
        {
            Axis.Row => new Vector2<float>(-GetScroll(), 0.0f),
            Axis.Column => new Vector2<float>(0.0f, -GetScroll()),
            _ => throw new ArgumentOutOfRangeException()
        }) * slot.Child.ComputeRelativeTransform();
    }

    public override bool OnCursorDown(CursorDownEvent e)
    {
        _mouseDownOffset = _offset;
        _mouseDownPos = e.Position.Cast<float>();
        _lastDownEvent = e;
        return true;
    }

    protected override bool OnCursorMove(CursorMoveEvent e)
    {
        if (_lastDownEvent?.Target == this)
        {
            var pos = _mouseDownPos - e.Position.Cast<float>();

            return ScrollTo(Axis switch
            {
                Axis.Row => _mouseDownOffset + pos.X,
                Axis.Column => _mouseDownOffset + pos.Y,
                _ => throw new ArgumentOutOfRangeException()
            });
            // var delta = Direction switch
            // {
            //     Axis.Horizontal => e.Delta.X,
            //     Axis.Vertical => e.Delta.Y * -1.0f,
            //     _ => throw new ArgumentOutOfRangeException()
            // };

            // return ScrollBy((float)delta);
        }

        return base.OnCursorMove(e);
    }

    public override void OnCursorUp(CursorUpEvent e)
    {
        base.OnCursorUp(e);
        _lastDownEvent = null;
    }
}