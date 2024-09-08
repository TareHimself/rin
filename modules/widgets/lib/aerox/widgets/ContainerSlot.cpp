#include "aerox/widgets/ContainerSlot.hpp"
namespace aerox::widgets
{
    Shared<Widget> ContainerSlot::GetWidget() const
    {
        return _widget;
    }
}
