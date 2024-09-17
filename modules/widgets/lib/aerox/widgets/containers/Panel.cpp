#include "aerox/widgets/containers/Panel.hpp"
#include "aerox/widgets/slots/PanelSlot.hpp"

namespace aerox::widgets
{
    Vec2<float> Panel::ComputeDesiredSize()
    {
        return Vec2{0.0f};
    }

    void Panel::ArrangeSlots(const Vec2<float>& drawSize)
    {
        for (auto &containerSlot : GetSlots())
        {
            containerSlot->As<PanelSlot>()->ComputeSizeAndOffset();
        } 
    }

    Shared<ContainerSlot> Panel::MakeSlot(const Shared<Widget>& widget)
    {
        return newShared<PanelSlot>(this,widget);
    }

    void Panel::OnChildResized(Widget* widget)
    {
        Container::OnChildResized(widget);
        if(auto slot = GetSlot(widget))
        {
            slot->As<PanelSlot>()->ComputeSizeAndOffset();
        }
    }
}
