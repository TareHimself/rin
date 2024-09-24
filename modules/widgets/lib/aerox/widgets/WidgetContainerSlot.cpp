#include "aerox/widgets/WidgetContainerSlot.hpp"
Shared<Widget> WidgetContainerSlot::GetWidget() const
{
    return _widget;
}

WidgetContainerSlot::WidgetContainerSlot(const Shared<Widget>& widget)
{
    _widget = widget;
}
