#include "rin/widgets/containers/WidgetPanel.hpp"
#include "rin/widgets/slots/WidgetPanelSlot.hpp"

Vec2<float> WidgetPanel::ComputeDesiredSize()
{
    return Vec2{0.0f};
}

void WidgetPanel::ArrangeSlots(const Vec2<float>& drawSize)
{
    for (auto &containerSlot : GetSlots())
    {
        containerSlot->As<WidgetPanelSlot>()->ComputeSizeAndOffset();
    } 
}

Shared<WidgetContainerSlot> WidgetPanel::MakeSlot(const Shared<Widget>& widget)
{
    return newShared<WidgetPanelSlot>(this,widget);
}

void WidgetPanel::OnChildResized(Widget* widget)
{
    WidgetContainer::OnChildResized(widget);
    if(auto slot = GetSlot(widget))
    {
        slot->As<WidgetPanelSlot>()->ComputeSizeAndOffset();
    }
}
