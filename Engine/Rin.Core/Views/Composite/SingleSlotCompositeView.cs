using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Views.Layouts;

namespace Rin.Core.Views.Composite;

public abstract class SingleSlotCompositeView : CompositeView, ISingleSlotCompositeView
{
    private SimpleSlot? _slot;
    private ISlot[] _slots = [];

    /// <summary>
    ///     Adds the View to this container
    /// </summary>
    public IView? InitChild
    {
        init => SetChild(value);
    }

    [PublicAPI]
    public void SetChild(IView? child)
    {
        _slot?.Child.SetParent(null);

        if (child != null)
        {
            if (_slot == null)
                _slot = new SimpleSlot
                {
                    Child = child
                };
            else
                _slot.Child = child;

            _slot.Child.SetParent(this);
            _slots = [_slot];
            InvalidateLayout();
        }
        else
        {
            _slot = null;
            _slots = [];
        }
    }

    [PublicAPI]
    public IView? GetChild()
    {
        return _slot?.Child;
    }

    [PublicAPI]
    public ISlot? GetSlot()
    {
        return _slot;
    }

    public override ISlot[] GetSlots()
    {
        return _slots;
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        if (GetSlot() is { } slot) return slot.Child.GetDesiredSize();

        return new Vector2();
    }
}