#include "rin/widgets/containers/WCScrollable.hpp"
#include "rin/widgets/ContainerWidgetSlot.hpp"
#include "rin/widgets/event/ScrollEvent.hpp"

void WCScrollable::ApplyScroll() const
{
    switch (GetAxis())
    {
    case WidgetAxis::Horizontal:
        {
            Vec2 offset{GetScroll() * -1.0f, 0.0f};

            for (auto& containerSlot : GetSlots())
            {
                auto widget = containerSlot->GetWidget();
                widget->SetOffset(offset);
                auto widgetDrawSize = widget->GetDesiredSize();
                offset = offset + Vec2{widgetDrawSize.x, 0.0f};
            }
        }
        break;
    case WidgetAxis::Vertical:
        {
            Vec2 offset{0.0f, GetScroll() * -1.0f};

            for (auto& containerSlot : GetSlots())
            {
                auto widget = containerSlot->GetWidget();
                widget->SetOffset(offset);
                auto widgetDrawSize = widget->GetDesiredSize();
                offset = offset + Vec2{0.0f, widgetDrawSize.y};
            }
        }
        break;
    }
}

WCScrollable::WCScrollable()
{
    
}

WCScrollable::WCScrollable(const WidgetAxis& axis) : WCList(axis)
{
    SetClipMode(EClipMode::Bounds);
}

bool WCScrollable::IsScrollable() const
{
    return GetMaxScroll() > 0.0f;
}

float WCScrollable::GetMaxScroll() const
{
    auto desiredSize = GetCachedDesiredSize().value_or(Vec2<float>{0, 0});
    switch (GetAxis())
    {
    case WidgetAxis::Horizontal:
        {
            return std::max(0.0f, desiredSize.x - GetSize().x);
        }
        break;
    case WidgetAxis::Vertical:
        {
            return std::max(0.0f, desiredSize.y - GetSize().y);
        }
        break;
    }

    return 0.0f;
}

float WCScrollable::GetScroll() const
{
    return _scroll;
}

float WCScrollable::GetScrollScale() const
{
    return _scrollScale;
}

void WCScrollable::SetScrollScale(float scale)
{
    _scrollScale = scale;
}

bool WCScrollable::OnScroll(const Shared<ScrollEvent>& event)
{
    if(IsScrollable())
    {
        float scrollDelta = 0.0f;
        switch (GetAxis())
        {
        case WidgetAxis::Horizontal:
            scrollDelta = event->delta.x;
            break;
        case WidgetAxis::Vertical:
            scrollDelta = -event->delta.y;
            break;
        }
        scrollDelta *= _scrollScale;
        
        const auto oldScroll = _scroll;
        _scroll = std::clamp<float>(_scroll + scrollDelta,0,GetMaxScroll());
        return oldScroll != _scroll;
    }
    
    return ContainerWidget::OnScroll(event);
}

bool WCScrollable::OnCursorDown(const Shared<CursorDownEvent>& event)
{
    return ContainerWidget::OnCursorDown(event);
}

TransformInfo WCScrollable::ComputeChildTransform(const Shared<Widget>& widget, const TransformInfo& myTransform)
{
    auto myTransformDup = myTransform;
    myTransformDup.transform = GetAxis() == WidgetAxis::Horizontal
                                   ? myTransformDup.transform.Translate(Vec2{-GetScroll(), 0.0f})
                                   : myTransformDup.transform.Translate(Vec2{0.0f, -GetScroll()});

    return WCList::ComputeChildTransform(widget, myTransformDup);
}
