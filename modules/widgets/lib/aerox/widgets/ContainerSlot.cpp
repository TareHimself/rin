#include "aerox/widgets/ContainerSlot.hpp"
namespace aerox::widgets
{
    Shared<Widget> ContainerSlot::GetWidget() const
    {
        return _widget;
    }

    ContainerSlot::ContainerSlot(const Shared<Widget>& widget)
    {
        _widget = widget;
    }
}
