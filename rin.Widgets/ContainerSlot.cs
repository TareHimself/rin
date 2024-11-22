using rin.Widgets.Enums;

namespace rin.Widgets;

public class ContainerSlot(ContainerWidget? owner = null)
{
    public required Widget Child { get; init; }
    
    public T? ChildAs<T>() where T : Widget
    {
        return Child is T castedWidget ? castedWidget : null;
    }

    public void SetOwner(ContainerWidget container)
    {
        owner = container;
    }

    public void Update()
    {
        owner?.OnSlotInvalidated(this, InvalidationType.Layout);
    }
}