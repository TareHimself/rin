#include "aerox/widgets/containers/WidgetScrollList.hpp"
#include "aerox/widgets/WidgetContainerSlot.hpp"

Shared<WidgetContainerSlot> WidgetScrollList::MakeSlot(const Shared<Widget>& widget)
    {
        return newShared<WidgetContainerSlot>(widget);
    }

    void WidgetScrollList::ApplyScroll() const
    {
        switch (GetAxis())
        {
        case Axis::Horizontal:
            {
                Vec2 offset{GetScroll() * -1.0f,0.0f};
                
                for (auto &containerSlot : GetSlots())
                {
                    auto widget = containerSlot->GetWidget();
                    widget->SetRelativeOffset(offset);
                    auto widgetDrawSize = widget->GetDesiredSize();
                    offset = offset + Vec2{widgetDrawSize.x,0.0f};
                }
            }
            break;
        case Axis::Vertical:
            {
                Vec2 offset{0.0f,GetScroll() * -1.0f};
                
                for (auto &containerSlot : GetSlots())
                {
                    auto widget = containerSlot->GetWidget();
                    widget->SetRelativeOffset(offset);
                    auto widgetDrawSize = widget->GetDesiredSize();
                    offset = offset + Vec2{0.0f,widgetDrawSize.y};
                }
            }
            break;
        }
    }

    WidgetScrollList::WidgetScrollList(const Axis& axis) : WidgetList(axis)
    {
        SetClipMode(EClipMode::Bounds);
    }

    bool WidgetScrollList::IsScrollable() const
    {
        return GetMaxScroll() > 0.0f;
    }

    float WidgetScrollList::GetMaxScroll() const
    {
        auto desiredSize = GetCachedDesiredSize().value_or(Vec2<float>{0,0});
        switch (GetAxis())
        {
        case Axis::Horizontal:
            {
                return std::max(0.0f,desiredSize.x - GetDrawSize().x);
            }
            break;
        case Axis::Vertical:
            {
                return std::max(0.0f,desiredSize.y - GetDrawSize().y);
            }
            break;
        }

        return 0.0f;
    }

    float WidgetScrollList::GetScroll() const
    {
        return _scroll;
    }

    bool WidgetScrollList::OnScroll(const Shared<ScrollEvent>& event)
    {
        return WidgetContainer::OnScroll(event);
    }

    bool WidgetScrollList::OnCursorDown(const Shared<CursorDownEvent>& event)
    {
        return WidgetContainer::OnCursorDown(event);
    }

    void WidgetScrollList::Collect(const TransformInfo& transform, std::vector<Shared<DrawCommand>>& drawCommands)
    {
        WidgetContainer::Collect(transform, drawCommands);
        if(IsScrollable())
        {
            
        }
    }

    TransformInfo WidgetScrollList::ComputeChildTransform(const Shared<Widget>& widget, const TransformInfo& myTransform)
    {
        auto myTransformDup = myTransform;
        myTransformDup.transform = GetAxis() == Axis::Vertical ? myTransformDup.transform.Translate(Vec2{-GetScroll(),0.0f}) : myTransformDup.transform.Translate(Vec2{0.0f,-GetScroll()});
        
        return WidgetList::ComputeChildTransform(widget, myTransformDup);
    }
