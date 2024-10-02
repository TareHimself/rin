#pragma once
#include "rin/widgets/ContainerWidget.hpp"
#include "rin/widgets/slots/PanelWidgetSlot.hpp"


class PanelWidget : public ContainerWidget
{
protected:
    static bool NearlyEqual(double a,double b, double tolerance = 0.001);
    Vec2<float> ComputeContentSize() override;
    void ArrangeSlots(const Vec2<float>& drawSize) override;
    Shared<ContainerWidgetSlot> MakeSlot(const Shared<Widget>& widget) override;
    void OnChildResized(Widget* widget) override;
    void OnChildSlotUpdated(ContainerWidgetSlot* slot) override;
public:
    using SlotType = PanelWidgetSlot;
};
