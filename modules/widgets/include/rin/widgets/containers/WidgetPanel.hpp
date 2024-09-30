#pragma once
#include "rin/widgets/WidgetContainer.hpp"
#include "rin/widgets/slots/WidgetPanelSlot.hpp"


class WidgetPanel : public WidgetContainer
{
protected:
    Vec2<float> ComputeContentSize() override;
    void ArrangeSlots(const Vec2<float>& drawSize) override;
    Shared<WidgetContainerSlot> MakeSlot(const Shared<Widget>& widget) override;
    void OnChildResized(Widget* widget) override;
public:
    using SlotType = WidgetPanelSlot;
};
