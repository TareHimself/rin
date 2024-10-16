#include "rin/widgets/containers/WCFlex.hpp"

#include "rin/widgets/slots/WSFlex.hpp"

Shared<ContainerWidgetSlot> WCFlex::MakeSlot(const Shared<Widget>& widget)
{
    return newShared<WSFlex>(this, widget);
}

void WCFlex::ArrangeSlots(const Vec2<float>& drawSize)
{
    auto totalRatio = 0.0f;
    std::vector<Shared<WSFlex>> flexSlots{};
    auto slots = GetSlots();
    flexSlots.reserve(slots.size());

    for (auto& slot : GetSlots())
    {
        if (auto asFlexSlot = std::dynamic_pointer_cast<WSFlex>(slot))
        {
            totalRatio += asFlexSlot->ratio;
            flexSlots.push_back(asFlexSlot);
        }
    }

    Vec2 offset{0.0f};

    switch (GetAxis())
    {
    case WidgetAxis::Horizontal:
        {
            for (auto& slot : flexSlots)
            {
                auto widget = slot->GetWidget();
                auto allotted = drawSize.x * (slot->ratio / totalRatio);
                widget->SetSize({allotted, drawSize.y});
                widget->SetOffset(offset);
                offset.x += allotted;
            }
        }
        break;
    case WidgetAxis::Vertical:
        {
            for (auto& slot : flexSlots)
            {
                auto widget = slot->GetWidget();
                auto allotted = drawSize.y * (slot->ratio / totalRatio);
                widget->SetSize({drawSize.x, allotted});
                widget->SetOffset(offset);
                offset.y += allotted;
            }
        }
        break;
    }
}

void WCFlex::OnChildSlotUpdated(ContainerWidgetSlot* slot)
{
    ArrangeSlots(GetContentSize());
}
