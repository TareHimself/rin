using rin.Framework.Views.Enums;

namespace rin.Framework.Views;

public class CompositeViewSlot(CompositeView? owner = null)
{
    public required View Child { get; init; }
    
    public T? ChildAs<T>() where T : View
    {
        return Child is T castedWidget ? castedWidget : null;
    }

    public void SetOwner(CompositeView composite)
    {
        owner = composite;
    }

    public void Update()
    {
        owner?.OnSlotInvalidated(this, InvalidationType.Layout);
    }
}