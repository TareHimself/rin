#include "rin/widgets/WidgetContainer.hpp"

#include "rin/widgets/WidgetContainerSlot.hpp"
#include "rin/widgets/event/CursorDownEvent.hpp"
#include "rin/widgets/event/CursorMoveEvent.hpp"
#include "rin/widgets/event/ScrollEvent.hpp"
#include "rin/widgets/graphics/WidgetDrawCommands.hpp"

void WidgetContainer::OnChildResized(Widget * widget)
    {
        if(CheckSize()) ArrangeSlots(GetContentSize());
    }

    Shared<WidgetContainerSlot> WidgetContainer::MakeSlot(const Shared<Widget>& widget)
    {
        return newShared<WidgetContainerSlot>(widget);
    }

    size_t WidgetContainer::GetMaxSlots() const
    {
        return std::numeric_limits<size_t>::max();
    }

    size_t WidgetContainer::GetUsedSlots() const
    {
        return _slots.size();
    }

    Shared<WidgetContainerSlot> WidgetContainer::GetSlot(int index) const
    {
        return index < _slots.size() ? _slots[index] : Shared<WidgetContainerSlot>{};
    }

    Shared<WidgetContainerSlot> WidgetContainer::GetSlot(Widget* widget) const
    {
        return _widgetsToSlots.contains(widget) ? _widgetsToSlots.at(widget) : Shared<WidgetContainerSlot>{};
    }

    std::vector<Shared<WidgetContainerSlot>> WidgetContainer::GetSlots() const
    {
        return _slots;
    }

    Shared<WidgetContainerSlot> WidgetContainer::AddChild(const Shared<Widget>& widget)
    {
        if(!widget) return {};
        
        auto newSlotCount = _slots.size() + 1;
        
        if(newSlotCount > GetMaxSlots()) return {};

        auto slot = MakeSlot(widget);
        _slots.push_back(slot);
        _widgetsToSlots.emplace(widget.get(),slot);
        widget->SetParent(this->GetSharedDynamic<WidgetContainer>());
        
        if(auto surf = GetSurface())
        {
            widget->NotifyAddedToSurface(surf);
        }

        OnChildResized(widget.get());
        
        return slot;
    }

    bool WidgetContainer::RemoveChild(const Shared<Widget>& widget)
    {
        if(!_widgetsToSlots.contains(widget.get())) return false;
        
        for(auto i = 0; i < _slots.size(); i++)
        {
            if(_slots[i]->GetWidget() != widget) continue;
            
            widget->SetParent({});
            
            if(auto surf = GetSurface())
            {
                widget->NotifyRemovedFromSurface(surf);
            }
            
            _slots.erase(_slots.begin() + i);
            _widgetsToSlots.erase(widget.get());

            CheckSize();
            return true;
        }

        return false;
    }

    void WidgetContainer::SetDrawSize(const Vec2<float>& size)
    {
        Widget::SetDrawSize(size);
        ArrangeSlots(GetContentSize());
    }

    void WidgetContainer::OnDispose(bool manual)
    {
        Widget::OnDispose(manual);
        
        for (auto &slot : GetSlots())
        {
            slot->GetWidget()->Dispose();
        }

        _slots.clear();
    }

    void WidgetContainer::NotifyAddedToSurface(const Shared<WidgetSurface>& widgetSurface)
    {
        Widget::NotifyAddedToSurface(widgetSurface);
        for(auto &slot : GetSlots())
        {
            slot->GetWidget()->NotifyAddedToSurface(widgetSurface);
        }
    }

    void WidgetContainer::NotifyRemovedFromSurface(const Shared<WidgetSurface>& widgetSurface)
    {
        Widget::NotifyRemovedFromSurface(widgetSurface);
        for(auto &slot : GetSlots())
        {
            slot->GetWidget()->NotifyRemovedFromSurface(widgetSurface);
        }
    }

    Shared<Widget> WidgetContainer::NotifyCursorDown(const Shared<CursorDownEvent>& event, const TransformInfo& transform)
    {
        if(AreChildrenHitTestable())
        {
            if(auto widget = NotifyChildrenCursorDown(event,transform)) return widget;
        }
        return Widget::NotifyCursorDown(event, transform);
    }

    void WidgetContainer::NotifyCursorEnter(const Shared<CursorMoveEvent>& event, const TransformInfo& transform,
                                      std::vector<Shared<Widget>>& items)
    {
        if(AreChildrenHitTestable()) NotifyChildrenCursorEnter(event,transform,items);
        
        Widget::NotifyCursorEnter(event, transform, items);
    }

    bool WidgetContainer::NotifyCursorMove(const Shared<CursorMoveEvent>& event, const TransformInfo& transform)
    {
        if(AreChildrenHitTestable()) return NotifyChildrenCursorMove(event,transform);
        return Widget::NotifyCursorMove(event, transform);
    }

    bool WidgetContainer::NotifyScroll(const Shared<ScrollEvent>& event, const TransformInfo& transform)
    {
        if(AreChildrenHitTestable()) return NotifyChildrenScroll(event,transform);
        return Widget::NotifyScroll(event, transform);
    }

    Shared<Widget> WidgetContainer::NotifyChildrenCursorDown(const Shared<CursorDownEvent>& event,
        const TransformInfo& transform)
    {
        for (auto &slot : GetSlots())
        {
            auto slotTransform = ComputeChildTransform(slot,transform);
            if(slotTransform.IsPointWithin(event->position) && slot->GetWidget()->NotifyCursorDown(event,slotTransform)) return slot->GetWidget();
        }
        return {};
    }

    bool WidgetContainer::NotifyChildrenCursorEnter(const Shared<CursorMoveEvent>& event, const TransformInfo& transform, std::vector<Shared<Widget>>& items)
    {
        for (auto &slot : GetSlots())
        {
            auto slotTransform = ComputeChildTransform(slot,transform);
            if(slotTransform.IsPointWithin(event->position))
            {
                slot->GetWidget()->NotifyCursorEnter(event,slotTransform,items);
            }
        }
        return false;
    }

    bool WidgetContainer::NotifyChildrenCursorMove(const Shared<CursorMoveEvent>& event, const TransformInfo& transform)
    {
        for (auto &slot : GetSlots())
        {
            auto slotTransform = ComputeChildTransform(slot,transform);
            if(slotTransform.IsPointWithin(event->position) && slot->GetWidget()->NotifyCursorMove(event,slotTransform)) return true;
        }
        return false;
    }

    bool WidgetContainer::NotifyChildrenScroll(const Shared<ScrollEvent>& event, const TransformInfo& transform)
    {
        for (auto &slot : GetSlots())
        {
            auto slotTransform = ComputeChildTransform(slot,transform);
            if(slotTransform.IsPointWithin(event->position) && slot->GetWidget()->NotifyScroll(event,slotTransform)) return true;
        }
        return false;
    }

    void WidgetContainer::Collect(const TransformInfo& transform, WidgetDrawCommands& drawCommands)
    {
        auto slots = GetSlots();
        auto clipMode = GetClipMode();
        if(clipMode == EClipMode::None)
        {
            for (auto &slot : slots)
            {
                TransformInfo t = ComputeChildTransform(slot,transform);
                auto widget = slot->GetWidget();
                widget->Collect(t,drawCommands);
            } 
        }
        else if(clipMode == EClipMode::Bounds)
        {
            drawCommands.PushClip(transform,this);

            const auto myAABR = transform.ComputeAxisAlignedBoundingRect();
            
            for (auto &slot : slots)
            {
                TransformInfo t = ComputeChildTransform(slot,transform);
                auto slotAABR = t.ComputeAxisAlignedBoundingRect();
                
                if(!myAABR.IntersectsWith(slotAABR)) continue;
                
                auto widget = slot->GetWidget();
                widget->Collect(t,drawCommands);
            }

            drawCommands.PopClip();
        }
    }

    TransformInfo WidgetContainer::ComputeChildTransform(const Shared<WidgetContainerSlot>& slot, const TransformInfo& myTransform)
    {
        return ComputeChildTransform(slot->GetWidget(),myTransform);
    }

    TransformInfo WidgetContainer::ComputeChildTransform(const Shared<Widget>& widget, const TransformInfo& myTransform)
    {
        auto padding = GetPadding();
        return TransformInfo{myTransform.transform.Translate(Vec2(padding.left,padding.top)) * widget->ComputeRelativeTransform(),widget->GetDrawSize(),myTransform.depth + 1};
    }

    EClipMode WidgetContainer::GetClipMode() const
    {
        return _clipMode;
    }

    void WidgetContainer::SetClipMode(EClipMode clipMode)
    {
        _clipMode = clipMode;
    }
