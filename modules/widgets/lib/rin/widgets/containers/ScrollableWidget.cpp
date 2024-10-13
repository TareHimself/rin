#include "rin/widgets/containers/ScrollableWidget.hpp"
#include "rin/widgets/ContainerWidgetSlot.hpp"
#include "rin/widgets/event/ScrollEvent.hpp"

void ScrollableWidget::ApplyScroll() const
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

ScrollableWidget::ScrollableWidget()
{
    
}

ScrollableWidget::ScrollableWidget(const WidgetAxis& axis) : ListWidget(axis)
{
    SetClipMode(EClipMode::Bounds);
}

bool ScrollableWidget::IsScrollable() const
{
    return GetMaxScroll() > 0.0f;
}

float ScrollableWidget::GetMaxScroll() const
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

float ScrollableWidget::GetScroll() const
{
    return _scroll;
}

bool ScrollableWidget::OnScroll(const Shared<ScrollEvent>& event)
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

    if(IsScrollable())
    {
        const auto oldScroll = _scroll;
        _scroll = std::clamp<float>(_scroll + scrollDelta,0,GetMaxScroll());
        return oldScroll != _scroll;
    }
    
    return ContainerWidget::OnScroll(event);
}

bool ScrollableWidget::OnCursorDown(const Shared<CursorDownEvent>& event)
{
    return ContainerWidget::OnCursorDown(event);
}

TransformInfo ScrollableWidget::ComputeChildTransform(const Shared<Widget>& widget, const TransformInfo& myTransform)
{
    auto myTransformDup = myTransform;
    myTransformDup.transform = GetAxis() == WidgetAxis::Horizontal
                                   ? myTransformDup.transform.Translate(Vec2{-GetScroll(), 0.0f})
                                   : myTransformDup.transform.Translate(Vec2{0.0f, -GetScroll()});

    return ListWidget::ComputeChildTransform(widget, myTransformDup);
}
