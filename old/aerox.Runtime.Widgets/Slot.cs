namespace aerox.Runtime.Widgets;

public class Slot(Widget widget)
{
    public Widget GetWidget()
    {
        return widget;
    }
    
    public T? GetWidget<T>() where T : Widget
    {
        return widget is T castedWidget ? castedWidget : null;
    }
}