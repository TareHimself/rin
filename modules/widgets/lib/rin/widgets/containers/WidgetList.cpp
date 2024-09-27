#include "rin/widgets/containers/WidgetList.hpp"
#include "rin/widgets/WidgetContainerSlot.hpp"

WidgetList::WidgetList(const Axis& axis) : WidgetContainer()
    {
        _axis = axis;
    }

    Axis WidgetList::GetAxis() const
    {
        return _axis;
    }

    void WidgetList::SetAxis(const Axis& axis)
    {
        auto oldAxis = _axis;
        _axis = axis;
        if(oldAxis != axis)
        {
            CheckSize();
        }
    }

    Shared<WidgetContainerSlot> WidgetList::MakeSlot(const Shared<Widget>& widget)
    {
        return newShared<WidgetContainerSlot>(widget);
    }

    void WidgetList::ArrangeSlots(const Vec2<float>& drawSize)
    {
        switch (GetAxis())
        {
        case Axis::Horizontal:
            {
                Vec2 offset{0.0f};
                
                for (auto &containerSlot : GetSlots())
                {
                    auto widget = containerSlot->GetWidget();
                    widget->SetRelativeOffset(offset);
                    auto widgetDrawSize = widget->GetDesiredSize();
                    widget->SetDrawSize(widgetDrawSize);
                    offset = offset + Vec2{widgetDrawSize.x,0.0f};
                }
            }
            break;
        case Axis::Vertical:
            {
                Vec2 offset{0.0f};
                
                for (auto &containerSlot : GetSlots())
                {
                    auto widget = containerSlot->GetWidget();
                    widget->SetRelativeOffset(offset);
                    auto widgetDrawSize = widget->GetDesiredSize();
                    widget->SetDrawSize(widgetDrawSize);
                    offset = offset + Vec2{0.0f,widgetDrawSize.y};
                }
            }
            break;
        }
    }

    Vec2<float> WidgetList::ComputeDesiredSize()
    {
        switch (GetAxis())
        {
        case Axis::Horizontal:
            {
                Vec2 result{0.0f};
                
                for (auto &containerSlot : GetSlots())
                {
                    auto size = containerSlot->GetWidget()->GetDesiredSize();
                    result.x += size.x;
                    result.y = std::max(result.y,size.y);
                }

                return result;
            }
            break;
        case Axis::Vertical:
            {
                Vec2 result{0.0f};
                
                for (auto &containerSlot : GetSlots())
                {
                    auto size = containerSlot->GetWidget()->GetDesiredSize();
                    result.y += size.y;
                    result.x = std::max(result.x,size.x);
                }

                return result;
            }
            break;
        }
        return Vec2{0.0f};
    }
