#pragma once
#include "rin/widgets/ContainerWidgetSlot.hpp"

class WSFlex : public ContainerWidgetSlot
{
public:
    float ratio = 1.0f;
    WSFlex(ContainerWidget* container, const Shared<Widget>& widget);
};
