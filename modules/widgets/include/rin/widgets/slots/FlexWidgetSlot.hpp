#pragma once
#include "rin/widgets/ContainerWidgetSlot.hpp"

class FlexWidgetSlot : public ContainerWidgetSlot
{
public:
    float ratio = 1.0f;
    FlexWidgetSlot(ContainerWidget* container, const Shared<Widget>& widget);
};
