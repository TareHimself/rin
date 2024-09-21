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
        using SlotType = ContainerSlot;
        virtual size_t GetMaxSlots() const;
        virtual size_t GetUsedSlots() const;
        virtual Shared<ContainerSlot> GetSlot(int index) const;
        virtual Shared<ContainerSlot> GetSlot(Widget * widget) const;
        virtual std::vector<Shared<ContainerSlot>> GetSlots() const;
        
        virtual Shared<ContainerSlot> AddChild(const Shared<Widget>& widget);
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

        template<typename T,typename ...TArgs>
        std::enable_if_t<std::is_constructible_v<T,TArgs...> && std::is_base_of_v<Widget,T>,Shared<ContainerSlot>> AddChild(TArgs&&... args);

        template<typename TSlotType,typename T,typename ...TArgs>
       std::enable_if_t<std::is_constructible_v<T,TArgs...> && std::is_base_of_v<Widget,T> && std::is_base_of_v<ContainerSlot,TSlotType>,Shared<TSlotType>> AddChild(TArgs&&... args);

        void Collect(const TransformInfo& transform, std::vector<Shared<DrawCommand>>& drawCommands) override;

        TransformInfo ComputeChildTransform(const Shared<ContainerSlot>& slot, const TransformInfo& myTransform);
        virtual TransformInfo ComputeChildTransform(const Shared<Widget>& widget, const TransformInfo& myTransform);
    };

    template <typename T, typename ... TArgs>
    std::enable_if_t<std::is_constructible_v<T, TArgs...> && std::is_base_of_v<Widget, T>, Shared<ContainerSlot>>
    Container::AddChild(TArgs&&... args)
    {
        return AddChild(newShared<T>(std::forward<TArgs>(args)...));
    }

    template <typename TSlotType, typename T, typename ... TArgs>
    std::enable_if_t<std::is_constructible_v<T, TArgs...> && std::is_base_of_v<Widget, T> && std::is_base_of_v<
    ContainerSlot, TSlotType>, Shared<TSlotType>> Container::AddChild(TArgs&&... args)
    {
        if(auto slot = AddChild<T>(std::forward<TArgs>(args)...))
        {
            return slot->template As<TSlotType>();
        }
        return Shared<TSlotType>{};
    }


    template<typename T,typename = std::enable_if_t<std::is_base_of_v<Container,T>>>
    std::pair<Shared<typename T::template SlotType>,Shared<T>> operator+(const Shared<T>& container,const Shared<Widget>& child)
    {
        auto slot = container->AddChild(child);
        return std::make_pair(slot->template As<T::template SlotType>(),container);
    }

    template<typename T,typename E,typename = std::enable_if_t<std::is_base_of_v<Container,T> && std::is_base_of_v<ContainerSlot,E>>>
    std::pair<Shared<typename T::template SlotType>,Shared<T>> operator+(std::pair<Shared<E>,Shared<T>>&& container,const Shared<Widget>& child)
    {
        auto slot = container.second->AddChild(child);
        return std::make_pair(slot->template As<T::template SlotType>(),container.second);
    }

    template<typename T,typename = std::enable_if_t<std::is_base_of_v<Container,T>>>
    Shared<T> operator-(const Shared<T>& container,const Shared<Widget>& child)
    {
        container->RemoveChild(child);
        return container;
    }
}
