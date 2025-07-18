﻿using System.Numerics;
using Rin.Engine.Math;
using Rin.Engine.Views.Events;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.Quads;
using Rin.Engine.Views.Layouts;

namespace Rin.Engine.Views.Composite;

/// <summary>
///     Slot = <see cref="Slot" />
/// </summary>
public class ScrollList : List
{
    private CursorDownSurfaceEvent? _lastDownEvent;
    private float _maxOffset;
    private float _mouseDownOffset;
    private Vector2 _mouseDownPos;
    private float _offset;

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

    public override void Collect(in Matrix4x4 transform, in Views.Rect clip, CommandList commands)
    {
        base.Collect(transform, clip, commands);
        if (IsVisible && IsScrollable())
        {
            var scroll = GetScroll();
            var maxScroll = GetMaxScroll();
            var axisSize = GetAxisSize();
            var desiredAxisSize = axisSize + maxScroll;

            var barSize = float.Max(MinBarSize, axisSize - (desiredAxisSize - axisSize));
            var availableDist = axisSize - barSize;
            var drawOffset = availableDist * (float.Max(scroll, 0.0001f) / maxScroll);

            var size = GetContentSize();

            var barTransform = transform.Translate(new Vector2(size.X - 10.0f, drawOffset));
            commands.AddRect(barTransform, new Vector2(10.0f, barSize), Color.White, new Vector4(7.0f));
        }
    }

    protected override Matrix4x4 ComputeSlotTransform(ISlot slot, in Matrix4x4 contentTransform)
    {
        return slot.Child.GetLocalTransform() * contentTransform.Translate(Axis switch
        {
            Axis.Row => new Vector2(-GetScroll(), 0.0f),
            Axis.Column => new Vector2(0.0f, -GetScroll()),
            _ => throw new ArgumentOutOfRangeException()
        });
    }

    public override bool OnCursorDown(CursorDownSurfaceEvent e)
    {
        _mouseDownOffset = _offset;
        _mouseDownPos = e.Position;
        _lastDownEvent = e;
        return true;
    }

    protected override bool OnCursorMove(CursorMoveSurfaceEvent e)
    {
        if (_lastDownEvent?.Target == this)
        {
            var pos = _mouseDownPos - e.Position;

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

        return false;
    }

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        base.OnCursorUp(e);
        _lastDownEvent = null;
    }
}