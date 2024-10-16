#include "rin/widgets/containers/WCRoot.hpp"

#include "rin/widgets/ContainerWidgetSlot.hpp"

Vec2<float> WCRoot::ComputeContentSize()
{
    return Vec2{0.0f};
}

void WCRoot::ArrangeSlots(const Vec2<float>& drawSize)
{
    for(const auto &slot : GetSlots())
    {
        const auto widget = slot->GetWidget();
        widget->SetOffset(Vec2{0.0f});
        widget->SetSize(drawSize);
    }
}
