﻿using System.Numerics;
using rin.Framework.Core.Math;
using rin.Framework.Views.Enums;
using rin.Framework.Views.Layouts;

namespace rin.Framework.Views.Composite;


/// <summary>
/// Slot = <see cref="Slot"/>
/// </summary>
public class Root : MultiSlotCompositeView<Slot>
{
    private readonly RootLayout _layout;

    public Root() : base()
    {
        _layout = new RootLayout(this);
    }
    
    protected override Vector2 ComputeDesiredContentSize()
    {
        return _layout.ComputeDesiredContentSize();
    }


    protected override Vector2 ArrangeContent(Vector2 availableSpace)
    {
        return _layout.Apply(availableSpace);
    }

    
    public override void OnChildInvalidated(View child, InvalidationType invalidation)
    {
        if (_layout.FindSlot(child) is { } slot)
        {
            _layout.OnSlotUpdated(slot);
        }
    }
    
    public override void OnChildAdded(View child)
    {
        if (_layout.FindSlot(child) is { } slot)
        {
            _layout.OnSlotUpdated(slot);
        }
    }

    public override IEnumerable<ISlot> GetSlots() => _layout.GetSlots();

    public override int SlotCount => _layout.SlotCount;
    public override bool Add(View view) => _layout.Add(view);
    public override bool Add(Slot slot) => _layout.Add(slot);
    public override bool Remove(View view) => _layout.Remove(view);

    
}