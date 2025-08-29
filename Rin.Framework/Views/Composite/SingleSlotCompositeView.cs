using System.Numerics;
using JetBrains.Annotations;
using Rin.Framework.Views.Enums;
using Rin.Framework.Views.Layouts;

namespace Rin.Framework.Views.Composite;

public class SimpleSlot : ISlot
{
    public required IView Child { get; set; }

    public void OnAddedToLayout(ILayout layout)
    {
    }

    public void OnRemovedFromLayout(ILayout layout)
    {
    }
}

public abstract class SingleSlotCompositeView : CompositeView, ISingleSlotCompositeView
{
    private SimpleSlot? _slot;

    /// <summary>
    ///     Adds the View to this container
    /// </summary>
    public IView? Child
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

            Invalidate(InvalidationType.Layout);
        }
        else
        {
            _slot = null;
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

    public override IEnumerable<ISlot> GetSlots()
    {
        if (GetSlot() is { } slot) return [slot];

        return [];
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        if (GetSlot() is { } slot) return slot.Child.GetDesiredSize();

        return new Vector2();
    }

    public override void OnChildInvalidated(IView child, InvalidationType invalidation)
    {
        Invalidate(invalidation);
    }

    // public override void OnChildInvalidated(View child, InvalidationType invalidation)
    // {
    //     Invalidate(invalidation);
    // }
}