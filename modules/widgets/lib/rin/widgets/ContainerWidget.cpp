#include "rin/widgets/ContainerWidget.hpp"

#include "rin/widgets/ContainerWidgetSlot.hpp"
#include "rin/widgets/event/CursorDownEvent.hpp"
#include "rin/widgets/event/CursorMoveEvent.hpp"
#include "rin/widgets/event/ScrollEvent.hpp"
#include "rin/widgets/graphics/WidgetDrawCommands.hpp"

void ContainerWidget::OnChildResized(Widget* widget)
{
    if (CheckSize()) ArrangeSlots(GetContentSize());
}

void ContainerWidget::OnChildSlotUpdated(ContainerWidgetSlot* slot)
{
    if (CheckSize()) ArrangeSlots(GetContentSize());
}

Shared<ContainerWidgetSlot> ContainerWidget::MakeSlot(const Shared<Widget>& widget)
{
    return newShared<ContainerWidgetSlot>(this, widget);
}

ContainerWidget::ContainerWidget()
{
    SetVisibility(WidgetVisibility::VisibleNoHitTestSelf);
}

size_t ContainerWidget::GetMaxSlots() const
{
    return std::numeric_limits<size_t>::max();
}

size_t ContainerWidget::GetUsedSlots() const
{
    return _slots.size();
}

Shared<ContainerWidgetSlot> ContainerWidget::GetSlot(int index) const
{
    return index < _slots.size() ? _slots[index] : Shared<ContainerWidgetSlot>{};
}

Shared<ContainerWidgetSlot> ContainerWidget::GetSlot(Widget* widget) const
{
    return _widgetsToSlots.contains(widget) ? _widgetsToSlots.at(widget) : Shared<ContainerWidgetSlot>{};
}

std::vector<Shared<ContainerWidgetSlot>> ContainerWidget::GetSlots() const
{
    return _slots;
}

Shared<ContainerWidgetSlot> ContainerWidget::AddChild(const Shared<Widget>& widget)
{
    if (!widget) return {};

    auto newSlotCount = _slots.size() + 1;

    if (newSlotCount > GetMaxSlots()) return {};

    auto slot = MakeSlot(widget);
    _slots.push_back(slot);
    _widgetsToSlots.emplace(widget.get(), slot);
    widget->SetParent(this->GetSharedDynamic<ContainerWidget>());

    if (auto surf = GetSurface())
    {
        widget->NotifyAddedToSurface(surf);
        OnChildResized(widget.get());
    }

    return slot;
}

bool ContainerWidget::RemoveChild(const Shared<Widget>& widget)
{
    if (!_widgetsToSlots.contains(widget.get())) return false;

    for (auto i = 0; i < _slots.size(); i++)
    {
        if (_slots[i]->GetWidget() != widget) continue;

        widget->SetParent({});

        auto surf = GetSurface();

        if (surf)
        {
            widget->NotifyRemovedFromSurface(surf);
        }

        _slots.erase(_slots.begin() + i);
        _widgetsToSlots.erase(widget.get());

        if (surf)
        {
            CheckSize();
        }

        return true;
    }

    return false;
}

void ContainerWidget::SetSize(const Vec2<float>& size)
{
    Widget::SetSize(size);
    if (auto surf = GetSurface())
    {
        ArrangeSlots(GetContentSize());
    }
}

void ContainerWidget::OnDispose(bool manual)
{
    Widget::OnDispose(manual);

    for (auto& slot : GetSlots())
    {
        slot->GetWidget()->Dispose();
    }

    _slots.clear();
}

void ContainerWidget::NotifyAddedToSurface(const Shared<WidgetSurface>& widgetSurface)
{
    Widget::NotifyAddedToSurface(widgetSurface);
    for (auto& slot : GetSlots())
    {
        slot->GetWidget()->NotifyAddedToSurface(widgetSurface);
    }
}

void ContainerWidget::NotifyRemovedFromSurface(const Shared<WidgetSurface>& widgetSurface)
{
    Widget::NotifyRemovedFromSurface(widgetSurface);
    for (auto& slot : GetSlots())
    {
        slot->GetWidget()->NotifyRemovedFromSurface(widgetSurface);
    }
}

Shared<Widget> ContainerWidget::NotifyCursorDown(const Shared<CursorDownEvent>& event, const TransformInfo& transform)
{
    if (AreChildrenHitTestable())
    {
        if (auto widget = NotifyChildrenCursorDown(event, transform)) return widget;
    }
    return Widget::NotifyCursorDown(event, transform);
}

void ContainerWidget::NotifyCursorEnter(const Shared<CursorMoveEvent>& event, const TransformInfo& transform,
                                        std::vector<Shared<Widget>>& items)
{
    if (AreChildrenHitTestable()) NotifyChildrenCursorEnter(event, transform, items);

    Widget::NotifyCursorEnter(event, transform, items);
}

bool ContainerWidget::NotifyCursorMove(const Shared<CursorMoveEvent>& event, const TransformInfo& transform)
{
    if (AreChildrenHitTestable() && NotifyChildrenCursorMove(event, transform)) return true;
    return Widget::NotifyCursorMove(event, transform);
}

bool ContainerWidget::NotifyScroll(const Shared<ScrollEvent>& event, const TransformInfo& transform)
{
    if (AreChildrenHitTestable() && NotifyChildrenScroll(event, transform)) return true;
    return Widget::NotifyScroll(event, transform);
}

Shared<Widget> ContainerWidget::NotifyChildrenCursorDown(const Shared<CursorDownEvent>& event,
                                                         const TransformInfo& transform)
{
    for (auto& slot : GetSlots())
    {
        if (auto slotTransform = ComputeChildTransform(slot, transform); slotTransform.IsPointWithin(event->position) && slot->GetWidget()->NotifyCursorDown(event, slotTransform))
            return slot->GetWidget();
    }
    return {};
}

bool ContainerWidget::NotifyChildrenCursorEnter(const Shared<CursorMoveEvent>& event, const TransformInfo& transform,
                                                std::vector<Shared<Widget>>& items)
{
    for (auto& slot : GetSlots())
    {
        if (auto slotTransform = ComputeChildTransform(slot, transform); slotTransform.IsPointWithin(event->position))
        {
            slot->GetWidget()->NotifyCursorEnter(event, slotTransform, items);
        }
    }
    return false;
}

bool ContainerWidget::NotifyChildrenCursorMove(const Shared<CursorMoveEvent>& event, const TransformInfo& transform)
{
    for (auto& slot : GetSlots())
    {
        if (auto slotTransform = ComputeChildTransform(slot, transform); slotTransform.IsPointWithin(event->position) && slot->GetWidget()->NotifyCursorMove(event, slotTransform))
            return true;
    }
    return false;
}

bool ContainerWidget::NotifyChildrenScroll(const Shared<ScrollEvent>& event, const TransformInfo& transform)
{
    for (auto& slot : GetSlots())
    {
        if (auto slotTransform = ComputeChildTransform(slot, transform); slotTransform.IsPointWithin(event->position) && slot->GetWidget()->NotifyScroll(event, slotTransform))
            return true;
    }
    return false;
}

void ContainerWidget::CollectContent(const TransformInfo& transform, WidgetDrawCommands& drawCommands)
{
    const auto slots = GetSlots();
    const auto clipMode = GetClipMode();
    if (clipMode == EClipMode::None)
    {
        for (auto& slot : slots)
        {
            TransformInfo t = ComputeChildTransform(slot, transform);
            const auto widget = slot->GetWidget();
            widget->Collect(t, drawCommands);
        }
    }
    else if (clipMode == EClipMode::Bounds)
    {
        drawCommands.PushClip(transform, this);

        const auto myAABR = transform.ComputeAxisAlignedBoundingRect();

        for (auto& slot : slots)
        {
            TransformInfo t = ComputeChildTransform(slot, transform);
            const auto slotAABR = t.ComputeAxisAlignedBoundingRect();

            if (!myAABR.IntersectsWith(slotAABR)) continue;

            const auto widget = slot->GetWidget();
            widget->Collect(t, drawCommands);
        }

        drawCommands.PopClip();
    }
}

TransformInfo ContainerWidget::ComputeChildTransform(const Shared<ContainerWidgetSlot>& slot,
                                                     const TransformInfo& myTransform)
{
    return ComputeChildTransform(slot->GetWidget(), myTransform);
}

TransformInfo ContainerWidget::ComputeChildTransform(const Shared<Widget>& widget, const TransformInfo& myTransform)
{
    auto padding = GetPadding();
    return TransformInfo{
        myTransform.transform.Translate(Vec2(padding.left, padding.top)) * widget->ComputeRelativeTransform(),
        widget->GetSize(), myTransform.depth + 1
    };
}

EClipMode ContainerWidget::GetClipMode() const
{
    return _clipMode;
}

void ContainerWidget::SetClipMode(EClipMode clipMode)
{
    _clipMode = clipMode;
}
