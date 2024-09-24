#pragma once
#include "aerox/widgets/WidgetContainer.hpp"
#include "aerox/widgets/slots/WidgetPanelSlot.hpp"


class WidgetPanel : public WidgetContainer
{
protected:
    Vec2<float> ComputeDesiredSize() override;
    void ArrangeSlots(const Vec2<float>& drawSize) override;
    Shared<WidgetContainerSlot> MakeSlot(const Shared<Widget>& widget) override;
    void OnChildResized(Widget* widget) override;
public:
    using SlotType = WidgetPanelSlot;
};
