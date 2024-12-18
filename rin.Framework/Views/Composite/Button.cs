using rin.Framework.Core.Animation;
using rin.Framework.Core.Extensions;
using rin.Framework.Core.Math;
using rin.Framework.Views.Animation;
using rin.Framework.Views.Events;

namespace rin.Framework.Views.Composite;

public class Button : Rect
{
    public event Action<CursorDownEvent,Button>? OnPressed; 
    public event Action<CursorUpEvent,Button>? OnReleased;

    public Button()
    {
        BackgroundColor = Color.Red; //Color.White.Clone(a: 0);
    }
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
            slot.Child.Offset = 0.0f;
            return slot.Child.ComputeSize(availableSpace);
        }

        return 0.0f;
    }

    public override bool OnCursorDown(CursorDownEvent e)
    {
        OnPressed?.Invoke(e,this);
        return (OnReleased?.GetInvocationList().NotEmpty() ?? false) || (OnPressed?.GetInvocationList().NotEmpty() ?? false);
    }

    public override void OnCursorUp(CursorUpEvent e)
    {
        base.OnCursorUp(e);
        OnReleased?.Invoke(e,this);
    }
}