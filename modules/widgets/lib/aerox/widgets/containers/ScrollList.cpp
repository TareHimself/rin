#include "aerox/widgets/containers/ScrollList.hpp"
#include "aerox/widgets/ContainerSlot.hpp"

namespace aerox::widgets
{
    Vec2<float> ScrollList::ComputeDesiredSize()
    {
        return _list->GetDesiredSize();
    }

    void ScrollList::ArrangeSlots(const Vec2<float>& drawSize)
    {
        List::ArrangeSlots(drawSize);
    }

    Shared<ContainerSlot> ScrollList::MakeSlot(const Shared<Widget>& widget)
    {
        return newShared<ContainerSlot>(widget);
    }

    void ScrollList::ApplyScroll() const
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

    ScrollList::ScrollList(const Axis& axis) : List(axis)
    {
        _list = newShared<List>(axis);
        Container::AddChild(_list);
    }

    bool ScrollList::IsScrollable() const
    {
        return GetMaxScroll() > 0.0f;
    }

    float ScrollList::GetMaxScroll() const
    {
        switch (_list->GetAxis())
        {
        case Axis::Horizontal:
            {
                return std::max(0.0f,_list->GetDrawSize().x - GetDrawSize().x);
            }
            break;
        case Axis::Vertical:
            {
                return std::max(0.0f,_list->GetDrawSize().y - GetDrawSize().y);
            }
            break;
        }

        return 0.0f;
    }

    float ScrollList::GetScroll() const
    {
        return _scroll;
    }

    bool ScrollList::OnScroll(const Shared<ScrollEvent>& event)
    {
        return Container::OnScroll(event);
    }

    bool ScrollList::OnCursorDown(const Shared<CursorDownEvent>& event)
    {
        return Container::OnCursorDown(event);
    }

    void ScrollList::Collect(const TransformInfo& transform, std::vector<Shared<DrawCommand>>& drawCommands)
    {
        Container::Collect(transform, drawCommands);
        if(IsScrollable())
        {
            
        }
    }

    TransformInfo ScrollList::ComputeChildTransform(const Shared<Widget>& widget, const TransformInfo& myTransform)
    {
        auto myTransformDup = myTransform;
        myTransformDup.transform = GetAxis() == Axis::Vertical ? myTransformDup.transform.Translate(Vec2{-GetScroll(),0.0f}) : myTransformDup.transform.Translate(Vec2{0.0f,-GetScroll()});
        
        return List::ComputeChildTransform(widget, myTransformDup);
    }
}
