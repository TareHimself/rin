#include "aerox/widgets/Container.hpp"

#include "aerox/widgets/ContainerSlot.hpp"
#include "aerox/widgets/event/CursorDownEvent.hpp"
#include "aerox/widgets/event/CursorMoveEvent.hpp"
#include "aerox/widgets/event/ScrollEvent.hpp"

namespace aerox::widgets
{
    void Container::OnChildResized(Widget * widget)
    {
        if(CheckSize()) ArrangeSlots(GetDrawSize());
    }

    size_t Container::GetMaxSlots() const
    {
        return std::numeric_limits<size_t>::max();
    }

    size_t Container::GetUsedSlots() const
    {
        return _slots.size();
    }

    Shared<ContainerSlot> Container::GetSlot(int index) const
    {
        return index < _slots.size() ? _slots[index] : Shared<ContainerSlot>{};
    }

    Shared<ContainerSlot> Container::GetSlot(Widget* widget) const
    {
        return _widgetsToSlots.contains(widget) ? _widgetsToSlots.at(widget) : Shared<ContainerSlot>{};
    }

    std::vector<Shared<ContainerSlot>> Container::GetSlots() const
    {
        return _slots;
    }

    Shared<ContainerSlot> Container::AddChild(const Shared<Widget>& widget)
    {
        if(!widget) return {};
        
        auto newSlotCount = _slots.size() + 1;
        
        if(newSlotCount > GetMaxSlots()) return {};

        auto slot = MakeSlot(widget);
        _slots.push_back(slot);
        _widgetsToSlots.emplace(widget.get(),slot);
        widget->SetParent(this->GetSharedDynamic<Container>());
        
        if(auto surf = GetSurface())
        {
            widget->NotifyAddedToSurface(surf);
        }

        OnChildResized(widget.get());
        
        return slot;
    }

    bool Container::RemoveChild(const Shared<Widget>& widget)
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

    void Container::SetDrawSize(const Vec2<float>& size)
    {
        Widget::SetDrawSize(size);
        ArrangeSlots(size);
    }

    void Container::OnDispose(bool manual)
    {
        Widget::OnDispose(manual);
        
        for (auto &slot : GetSlots())
        {
            slot->GetWidget()->Dispose();
        }

        _slots.clear();
    }

    void Container::NotifyAddedToSurface(const Shared<Surface>& widgetSurface)
    {
        Widget::NotifyAddedToSurface(widgetSurface);
        for(auto &slot : GetSlots())
        {
            slot->GetWidget()->NotifyAddedToSurface(widgetSurface);
        }
    }

    void Container::NotifyRemovedFromSurface(const Shared<Surface>& widgetSurface)
    {
        Widget::NotifyRemovedFromSurface(widgetSurface);
        for(auto &slot : GetSlots())
        {
            slot->GetWidget()->NotifyRemovedFromSurface(widgetSurface);
        }
    }

    Shared<Widget> Container::NotifyCursorDown(const Shared<CursorDownEvent>& event, const TransformInfo& transform)
    {
        if(AreChildrenHitTestable())
        {
            if(auto widget = NotifyChildrenCursorDown(event,transform)) return widget;
        }
        return Widget::NotifyCursorDown(event, transform);
    }

    void Container::NotifyCursorEnter(const Shared<CursorMoveEvent>& event, const TransformInfo& transform,
                                      std::vector<Shared<Widget>>& items)
    {
        if(AreChildrenHitTestable()) NotifyChildrenCursorEnter(event,transform,items);
        
        Widget::NotifyCursorEnter(event, transform, items);
    }

    bool Container::NotifyCursorMove(const Shared<CursorMoveEvent>& event, const TransformInfo& transform)
    {
        if(AreChildrenHitTestable()) return NotifyChildrenCursorMove(event,transform);
        return Widget::NotifyCursorMove(event, transform);
    }

    bool Container::NotifyScroll(const Shared<ScrollEvent>& event, const TransformInfo& transform)
    {
        if(AreChildrenHitTestable()) return NotifyChildrenScroll(event,transform);
        return Widget::NotifyScroll(event, transform);
    }

    Shared<Widget> Container::NotifyChildrenCursorDown(const Shared<CursorDownEvent>& event,
        const TransformInfo& transform)
    {
        for (auto &slot : GetSlots())
        {
            auto slotTransform = transform.AccountFor(slot);
            if(slotTransform.IsPointWithin(event->position) && slot->GetWidget()->NotifyCursorDown(event,slotTransform)) return slot->GetWidget();
        }
        return {};
    }

    bool Container::NotifyChildrenCursorEnter(const Shared<CursorMoveEvent>& event, const TransformInfo& transform, std::vector<Shared<Widget>>& items)
    {
        for (auto &slot : GetSlots())
        {
            auto slotTransform = transform.AccountFor(slot);
            if(slotTransform.IsPointWithin(event->position))
            {
                slot->GetWidget()->NotifyCursorEnter(event,slotTransform,items);
            }
        }
        return false;
    }

    bool Container::NotifyChildrenCursorMove(const Shared<CursorMoveEvent>& event, const TransformInfo& transform)
    {
        for (auto &slot : GetSlots())
        {
            auto slotTransform = transform.AccountFor(slot);
            if(slotTransform.IsPointWithin(event->position) && slot->GetWidget()->NotifyCursorMove(event,slotTransform)) return true;
        }
        return false;
    }

    bool Container::NotifyChildrenScroll(const Shared<ScrollEvent>& event, const TransformInfo& transform)
    {
        for (auto &slot : GetSlots())
        {
            auto slotTransform = transform.AccountFor(slot);
            if(slotTransform.IsPointWithin(event->position) && slot->GetWidget()->NotifyScroll(event,slotTransform)) return true;
        }
        return false;
    }

    void Container::Collect(const TransformInfo& transform, std::vector<Shared<DrawCommand>>& drawCommands)
    {
        for (auto &slot : GetSlots())
        {
            auto widget = slot->GetWidget();
            TransformInfo t = transform.AccountFor(widget);
            widget->Collect(t,drawCommands);
        } 
    }
}
