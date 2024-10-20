#include "rin/widgets/containers/WCSwitcher.hpp"
#include "rin/widgets/ContainerWidgetSlot.hpp"
#include "rin/widgets/event/CursorDownEvent.hpp"
#include "rin/widgets/event/CursorMoveEvent.hpp"
#include "rin/widgets/event/ScrollEvent.hpp"
#include "rin/widgets/graphics/WidgetDrawCommands.hpp"

int WCSwitcher::GetSelectedIndex() const
{
    return _selectedIndex;
}

void WCSwitcher::SetSelectedIndex(const int index)
{
    const auto lastIndex = _selectedIndex;
    _selectedIndex = index;
    if(lastIndex != _selectedIndex)
    {
        CheckSize();
        ArrangeSlots(GetContentSize());
    }
}

Vec2<float> WCSwitcher::ComputeContentSize()
{
    if (const auto activeWidget = GetActiveSlot())
    {
        return activeWidget->GetWidget()->GetDesiredSize();
    }

    return Vec2{0.0f};
}

void WCSwitcher::ArrangeSlots(const Vec2<float>& drawSize)
{
    if (const auto activeWidget = GetActiveWidget())
    {
        activeWidget->SetOffset(Vec2{0.0f});
        activeWidget->SetSize(drawSize);
    }
}

Shared<ContainerWidgetSlot> WCSwitcher::GetActiveSlot() const
{
    if (const auto slots = GetSlots(); slots.size() > GetSelectedIndex())
    {
        return slots.at(GetSelectedIndex());
    }

    return {};
}

Shared<Widget> WCSwitcher::GetActiveWidget() const
{
    if (const auto slot = GetActiveSlot())
    {
        return slot->GetWidget();
    }

    return {};
}

Shared<Widget> WCSwitcher::NotifyChildrenCursorDown(const Shared<CursorDownEvent>& event,
                                                    const TransformInfo& transform)
{
    if (const auto slot = GetActiveSlot())
    {
        if (const auto slotTransform = ComputeChildTransform(slot, transform); slotTransform.
            IsPointWithin(event->position) && slot->GetWidget()->NotifyCursorDown(event, slotTransform))
            return slot->GetWidget();
    }
    return {};
}

bool WCSwitcher::NotifyChildrenCursorEnter(const Shared<CursorMoveEvent>& event, const TransformInfo& transform,
                                           std::vector<Shared<Widget>>& items)
{
    if (const auto slot = GetActiveSlot())
    {
        if (const auto slotTransform = ComputeChildTransform(slot, transform); slotTransform.IsPointWithin(
            event->position))
        {
            slot->GetWidget()->NotifyCursorEnter(event, slotTransform, items);
        }
    }
    return false;
}

bool WCSwitcher::NotifyChildrenCursorMove(const Shared<CursorMoveEvent>& event, const TransformInfo& transform)
{
    if (const auto slot = GetActiveSlot())
    {
        const auto slotTransform = ComputeChildTransform(slot, transform);
        return slotTransform.IsPointWithin(event->position) && slot->GetWidget()->
                                                                     NotifyCursorMove(event, slotTransform);
    }
    return false;
}

bool WCSwitcher::NotifyChildrenScroll(const Shared<ScrollEvent>& event, const TransformInfo& transform)
{
    if (const auto slot = GetActiveSlot())
    {
        const auto slotTransform = ComputeChildTransform(slot, transform);
        return slotTransform.IsPointWithin(event->position) && slot->GetWidget()->NotifyScroll(event, slotTransform);
    }
    return false;
}

void WCSwitcher::CollectContent(const TransformInfo& transform, WidgetDrawCommands& drawCommands)
{
    const auto slots = GetSlots();
    const auto clipMode = GetClipMode();
    if (clipMode == EClipMode::None)
    {
        if(const auto slot = GetActiveSlot())
        {
            const TransformInfo slotTransform = ComputeChildTransform(slot, transform);
            const auto widget = slot->GetWidget();
            widget->Collect(slotTransform, drawCommands);
        }
    }
    else if (clipMode == EClipMode::Bounds)
    {
        drawCommands.PushClip(transform, this);

        const auto myAABR = transform.ComputeAxisAlignedBoundingRect();

        if(const auto slot = GetActiveSlot())
        {
            const TransformInfo slotTransform = ComputeChildTransform(slot, transform);
            const auto slotAABR = slotTransform.ComputeAxisAlignedBoundingRect();
            if (myAABR.IntersectsWith(slotAABR))
            {
                const auto widget = slot->GetWidget();
                widget->Collect(slotTransform, drawCommands);
            }
        }
        drawCommands.PopClip();
    }
}
