﻿#pragma once
#include "Widget.hpp"
#include <unordered_map>
#include "rin/core/Disposable.hpp"

enum class EClipMode
    {
        None,
        Bounds,
        Custom
    };
    
    class WidgetContainer : public Widget
    {
        std::unordered_map<Widget *,Shared<WidgetContainerSlot>> _widgetsToSlots{};
        std::vector<Shared<WidgetContainerSlot>> _slots{};
        EClipMode _clipMode = EClipMode::None;
        friend Widget;
    protected:
        virtual void ArrangeSlots(const Vec2<float>& drawSize) = 0;
        virtual void OnChildResized(Widget * widget);
        virtual Shared<WidgetContainerSlot> MakeSlot(const Shared<Widget>& widget);
    public:
        using SlotType = WidgetContainerSlot;
        virtual size_t GetMaxSlots() const;
        virtual size_t GetUsedSlots() const;
        virtual Shared<WidgetContainerSlot> GetSlot(int index) const;
        virtual Shared<WidgetContainerSlot> GetSlot(Widget * widget) const;
        virtual std::vector<Shared<WidgetContainerSlot>> GetSlots() const;
        
        virtual Shared<WidgetContainerSlot> AddChild(const Shared<Widget>& widget);
        bool RemoveChild(const Shared<Widget>& widget);

        void SetDrawSize(const Vec2<float>& size) override;

        void OnDispose(bool manual) override;

        void NotifyAddedToSurface(const Shared<WidgetSurface>& widgetSurface) override;
        void NotifyRemovedFromSurface(const Shared<WidgetSurface>& widgetSurface) override;
        Shared<Widget> NotifyCursorDown(const Shared<CursorDownEvent>& event, const TransformInfo& transform) override;
        void NotifyCursorEnter(const Shared<CursorMoveEvent>& event, const TransformInfo& transform, std::vector<Shared<Widget>>& items) override;
        bool NotifyCursorMove(const Shared<CursorMoveEvent>& event, const TransformInfo& transform) override;
        bool NotifyScroll(const Shared<ScrollEvent>& event, const TransformInfo& transform) override;

        virtual Shared<Widget> NotifyChildrenCursorDown(const Shared<CursorDownEvent>& event, const TransformInfo& transform);
        virtual bool NotifyChildrenCursorEnter(const Shared<CursorMoveEvent>& event, const TransformInfo& transform, std::vector<Shared<Widget>>& items);
        virtual bool NotifyChildrenCursorMove(const Shared<CursorMoveEvent>& event, const TransformInfo& transform);
        virtual bool NotifyChildrenScroll(const Shared<ScrollEvent>& event, const TransformInfo& transform);

        template<typename T,typename ...TArgs>
        std::enable_if_t<std::is_constructible_v<T,TArgs...> && std::is_base_of_v<Widget,T>,Shared<WidgetContainerSlot>> AddChild(TArgs&&... args);

        template<typename TSlotType,typename T,typename ...TArgs>
       std::enable_if_t<std::is_constructible_v<T,TArgs...> && std::is_base_of_v<Widget,T> && std::is_base_of_v<WidgetContainerSlot,TSlotType>,Shared<TSlotType>> AddChild(TArgs&&... args);

        void Collect(const TransformInfo& transform, WidgetDrawCommands& drawCommands) override;

        TransformInfo ComputeChildTransform(const Shared<WidgetContainerSlot>& slot, const TransformInfo& myTransform);
        virtual TransformInfo ComputeChildTransform(const Shared<Widget>& widget, const TransformInfo& myTransform);

        EClipMode GetClipMode() const;

        void SetClipMode(EClipMode clipMode);

        
    };

    template <typename T, typename ... TArgs>
    std::enable_if_t<std::is_constructible_v<T, TArgs...> && std::is_base_of_v<Widget, T>, Shared<WidgetContainerSlot>>
    WidgetContainer::AddChild(TArgs&&... args)
    {
        return AddChild(newShared<T>(std::forward<TArgs>(args)...));
    }

    template <typename TSlotType, typename T, typename ... TArgs>
    std::enable_if_t<std::is_constructible_v<T, TArgs...> && std::is_base_of_v<Widget, T> && std::is_base_of_v<
    WidgetContainerSlot, TSlotType>, Shared<TSlotType>> WidgetContainer::AddChild(TArgs&&... args)
    {
        if(auto slot = AddChild<T>(std::forward<TArgs>(args)...))
        {
            return slot->template As<TSlotType>();
        }
        return Shared<TSlotType>{};
    }


    template<typename T,typename = std::enable_if_t<std::is_base_of_v<WidgetContainer,T>>>
    std::pair<Shared<typename T::template SlotType>,Shared<T>> operator+(const Shared<T>& container, const Shared<Widget>& other)
    {
        auto slot = container->AddChild(other);
        return std::make_pair(slot->template As<T::template SlotType>(),container);
    }

    
    template<typename T,typename = std::enable_if_t<std::is_base_of_v<WidgetContainer,T>>>
    std::pair<Shared<typename T::template SlotType>,Shared<T>> operator+(const std::pair<Shared<typename T::template SlotType>,Shared<T>>& container,const Shared<Widget>& other)
    {
        auto slot = container.second->AddChild(other);
        return std::make_pair(slot->template As<T::template SlotType>(),container.second);
    }

    template<typename T,typename E,typename = std::enable_if_t<std::is_base_of_v<WidgetContainer,T> && std::is_base_of_v<WidgetContainer,E>>>
    std::pair<Shared<typename T::template SlotType>,Shared<T>> operator+(const Shared<T>& container,const std::pair<Shared<typename E::template SlotType>,Shared<E>>& other)
    {
        auto slot = container->AddChild(other.second);
        return std::make_pair(slot->template As<T::template SlotType>(),container.second);
    }

    template<typename T,typename = std::enable_if_t<std::is_base_of_v<WidgetContainer,T>>>
    Shared<T> operator-(const Shared<T>& container,const Shared<Widget>& child)
    {
        container->RemoveChild(child);
        return container;
    }