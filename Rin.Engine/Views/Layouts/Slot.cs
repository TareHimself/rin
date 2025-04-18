﻿namespace Rin.Engine.Views.Layouts;

public class Slot(ILayout? owner = null) : ISlot
{
    private ILayout? _owner = owner;
    public required View Child { get; set; }

    public void OnAddedToLayout(ILayout layout)
    {
        _owner = layout;
    }

    public void OnRemovedFromLayout(ILayout layout)
    {
        _owner = null;
    }

    public T? ChildAs<T>() where T : View
    {
        return Child is T castedView ? castedView : null;
    }

    public void Apply()
    {
        _owner?.OnSlotUpdated(this);
    }
}