﻿#include "rin/widgets/containers/WCList.hpp"
#include "rin/widgets/ContainerWidgetSlot.hpp"

WCList::WCList(const WidgetAxis& axis) : ContainerWidget()
{
    _axis = axis;
}

WidgetAxis WCList::GetAxis() const
{
    return _axis;
}

void WCList::SetAxis(const WidgetAxis& axis)
{
    auto oldAxis = _axis;
    _axis = axis;
    if (oldAxis != axis)
    {
        CheckSize();
    }
}

void WCList::ArrangeSlots(const Vec2<float>& drawSize)
{
    switch (GetAxis())
    {
    case WidgetAxis::Horizontal:
        {
            Vec2 offset{0.0f};

            for (auto& containerSlot : GetSlots())
            {
                auto widget = containerSlot->GetWidget();
                widget->SetOffset(offset);
                auto widgetDrawSize = widget->GetDesiredSize();
                widgetDrawSize.y = drawSize.y;
                widget->SetSize(widgetDrawSize);
                offset = offset + Vec2{widgetDrawSize.x, 0.0f};
            }
        }
        break;
    case WidgetAxis::Vertical:
        {
            Vec2 offset{0.0f};

            for (auto& containerSlot : GetSlots())
            {
                auto widget = containerSlot->GetWidget();
                widget->SetOffset(offset);
                auto widgetDrawSize = widget->GetDesiredSize();
                widgetDrawSize.x = drawSize.x;
                widget->SetSize(widgetDrawSize);
                offset = offset + Vec2{0.0f, widgetDrawSize.y};
            }
        }
        break;
    }
}

Vec2<float> WCList::ComputeContentSize()
{
    switch (GetAxis())
    {
    case WidgetAxis::Horizontal:
        {
            Vec2 result{0.0f};

            for (auto& containerSlot : GetSlots())
            {
                auto size = containerSlot->GetWidget()->GetDesiredSize();
                result.x += size.x;
                result.y = std::max(result.y, size.y);
            }

            return result;
        }
        break;
    case WidgetAxis::Vertical:
        {
            Vec2 result{0.0f};

            for (auto& containerSlot : GetSlots())
            {
                auto size = containerSlot->GetWidget()->GetDesiredSize();
                result.y += size.y;
                result.x = std::max(result.x, size.x);
            }

            return result;
        }
        break;
    }
    return Vec2{0.0f};
}
