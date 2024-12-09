﻿using rin.Framework.Core.Math;
using rin.Framework.Widgets.Events;

namespace rin.Framework.Widgets.Containers;

public class Button : Containers.Rect
{

    public event Action<CursorDownEvent>? OnPressed; 
    public event Action<CursorUpEvent>? OnReleased; 
    protected override Vector2<float> ComputeDesiredContentSize()
    {
        if (GetSlot(0) is { } slot)
        {
            return slot.Child.GetDesiredSize();
        }
        return new Vector2<float>();
    }

    public override int GetMaxSlotsCount() => 1;
    
    protected override Vector2<float> ArrangeContent(Vector2<float> availableSpace)
    {
        if (GetSlot(0) is { } slot)
        {
            slot.Child.Offset = (new Vector2<float>(0.0f));
            return slot.Child.ComputeSize(availableSpace);
        }

        return 0.0f;
    }

    protected override bool OnCursorDown(CursorDownEvent e)
    {
        OnPressed?.Invoke(e);
        return true;
    }

    public override void OnCursorUp(CursorUpEvent e)
    {
        base.OnCursorUp(e);
        OnReleased?.Invoke(e);
    }
}