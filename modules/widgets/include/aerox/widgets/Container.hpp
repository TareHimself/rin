#pragma once
#include "Widget.hpp"
#include <unordered_map>
#include "aerox/core/Disposable.hpp"

namespace aerox::widgets
{
    
    class Container : public Widget
    {
        std::unordered_map<Widget *,Shared<ContainerSlot>> _widgetsToSlots{};
        std::vector<Shared<ContainerSlot>> _slots{};
        friend Widget;
    protected:
        virtual void ArrangeSlots(const Vec2<float>& drawSize) = 0;
        virtual void OnChildResized(Widget * widget);
        virtual Shared<ContainerSlot> MakeSlot(const Shared<Widget>& widget) = 0;
    public:
        virtual size_t GetMaxSlots() const = 0;
        virtual size_t GetNumSlots() const;
        virtual Shared<ContainerSlot> GetSlot(int index) const;
        virtual std::vector<Shared<ContainerSlot>> GetSlots() const;
        
        Shared<ContainerSlot> AddChild(const Shared<Widget>& widget);
        bool RemoveChild(const Shared<Widget>& widget);

        void SetDrawSize(const Vec2<float>& size) override;

        void OnDispose(bool manual) override;

        void NotifyAddedToSurface(const Shared<Surface>& widgetSurface) override;
        void NotifyRemovedFromSurface(const Shared<Surface>& widgetSurface) override;
        Shared<Widget> NotifyCursorDown(const Shared<CursorDownEvent>& event, const TransformInfo& transform) override;
        void NotifyCursorEnter(const Shared<CursorMoveEvent>& event, const TransformInfo& transform, std::vector<Shared<Widget>>& items) override;
        bool NotifyCursorMove(const Shared<CursorMoveEvent>& event, const TransformInfo& transform) override;
        bool NotifyScroll(const Shared<ScrollEvent>& event, const TransformInfo& transform) override;

        virtual Shared<Widget> NotifyChildrenCursorDown(const Shared<CursorDownEvent>& event, const TransformInfo& transform);
        virtual bool NotifyChildrenCursorEnter(const Shared<CursorMoveEvent>& event, const TransformInfo& transform, std::vector<Shared<Widget>>& items);
        virtual bool NotifyChildrenCursorMove(const Shared<CursorMoveEvent>& event, const TransformInfo& transform);
        virtual bool NotifyChildrenScroll(const Shared<ScrollEvent>& event, const TransformInfo& transform);
    };


    template<typename T,std::enable_if_t<std::is_base_of_v<Container,T>>*>
    Shared<T> operator+(const Shared<Container>& container,const Shared<Widget>& child)
    {
        container->AddChild(child);
        return container;
    }

    template<typename T,std::enable_if_t<std::is_base_of_v<Container,T>>*>
    Shared<T> operator-(const Shared<Container>& container,const Shared<Widget>& child)
    {
        container->RemoveChild(child);
        return container;
    }
}
