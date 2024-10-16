#pragma once
#include "WCList.hpp"
#include "rin/widgets/ContainerWidget.hpp"

class WCFlex : public WCList
{
public:
    Shared<ContainerWidgetSlot> MakeSlot(const Shared<Widget>& widget) override;
    void ArrangeSlots(const Vec2<float>& drawSize) override;
    void OnChildSlotUpdated(ContainerWidgetSlot* slot) override;
};
