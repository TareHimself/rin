namespace aerox.Runtime.Widgets;

public class Slot(Widget widget,Container? owner = null)
{
    public Widget GetWidget()
    {
        return widget;
    }
    
    public T? GetWidget<T>() where T : Widget
    {
        return widget is T castedWidget ? castedWidget : null;
    }

    public void SetOwner(Container container)
    {
        owner = container;
    }

    public void Update()
    {
        owner.OnSlotUpdated(this);
    }
}