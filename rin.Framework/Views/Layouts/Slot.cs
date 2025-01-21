namespace rin.Framework.Views.Layouts;

public class Slot(ILayout? owner = null) : ISlot
{
    private ILayout? _owner = owner;
    public required View Child { get; set; }
    
    public T? ChildAs<T>() where T : View
    {
        return Child is T castedView ? castedView : null;
    }

    public void SetLayout(ILayout layout)
    {
        _owner = layout;
    }

    public void Apply()
    {
        _owner?.OnSlotUpdated(this);
    }
}