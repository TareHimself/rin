#pragma once
#include "ListWidget.hpp"
#include "rin/widgets/ContainerWidget.hpp"

class FlexWidget : public ListWidget
{
public:
    Shared<ContainerWidgetSlot> MakeSlot(const Shared<Widget>& widget) override;
    void ArrangeSlots(const Vec2<float>& drawSize) override;
    void OnChildSlotUpdated(ContainerWidgetSlot* slot) override;
};
