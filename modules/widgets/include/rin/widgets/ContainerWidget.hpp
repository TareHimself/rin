#pragma once
#include "Widget.hpp"
#include <unordered_map>
#include "rin/core/Disposable.hpp"

enum class EClipMode
{
    None,
    Bounds,
    Custom
};

class ContainerWidget : public Widget
{
    std::unordered_map<Widget*, Shared<ContainerWidgetSlot>> _widgetsToSlots{};
    std::vector<Shared<ContainerWidgetSlot>> _slots{};
    EClipMode _clipMode = EClipMode::None;
    friend Widget;
    friend ContainerWidgetSlot;

protected:
    virtual void ArrangeSlots(const Vec2<float>& drawSize) = 0;
    virtual void OnChildResized(Widget* widget);
    virtual void OnChildSlotUpdated(ContainerWidgetSlot* slot);
    virtual Shared<ContainerWidgetSlot> MakeSlot(const Shared<Widget>& widget);

public:
    ContainerWidget();
    using SlotType = ContainerWidgetSlot;
    virtual size_t GetMaxSlots() const;
    virtual size_t GetUsedSlots() const;
    virtual Shared<ContainerWidgetSlot> GetSlot(int index) const;
    virtual Shared<ContainerWidgetSlot> GetSlot(Widget* widget) const;
    virtual std::vector<Shared<ContainerWidgetSlot>> GetSlots() const;

    virtual Shared<ContainerWidgetSlot> AddChild(const Shared<Widget>& widget);
    bool RemoveChild(const Shared<Widget>& widget);

    void SetSize(const Vec2<float>& size) override;

    void OnDispose(bool manual) override;

    void NotifyAddedToSurface(const Shared<WidgetSurface>& widgetSurface) override;
    void NotifyRemovedFromSurface(const Shared<WidgetSurface>& widgetSurface) override;
    Shared<Widget> NotifyCursorDown(const Shared<CursorDownEvent>& event, const TransformInfo& transform) override;
    void NotifyCursorEnter(const Shared<CursorMoveEvent>& event, const TransformInfo& transform,
                           std::vector<Shared<Widget>>& items) override;
    bool NotifyCursorMove(const Shared<CursorMoveEvent>& event, const TransformInfo& transform) override;
    bool NotifyScroll(const Shared<ScrollEvent>& event, const TransformInfo& transform) override;

    virtual Shared<Widget> NotifyChildrenCursorDown(const Shared<CursorDownEvent>& event,
                                                    const TransformInfo& transform);
    virtual bool NotifyChildrenCursorEnter(const Shared<CursorMoveEvent>& event, const TransformInfo& transform,
                                           std::vector<Shared<Widget>>& items);
    virtual bool NotifyChildrenCursorMove(const Shared<CursorMoveEvent>& event, const TransformInfo& transform);
    virtual bool NotifyChildrenScroll(const Shared<ScrollEvent>& event, const TransformInfo& transform);

    template <typename T, typename... TArgs>
    std::enable_if_t<std::is_constructible_v<T, TArgs...> && std::is_base_of_v<Widget, T>, Shared<ContainerWidgetSlot>>
    AddChild(TArgs&&... args);

    template <typename TSlotType, typename T, typename... TArgs>
    std::enable_if_t<std::is_constructible_v<T, TArgs...> && std::is_base_of_v<Widget, T> && std::is_base_of_v<
                         ContainerWidgetSlot, TSlotType>, Shared<TSlotType>> AddChild(TArgs&&... args);
    
    void CollectContent(const TransformInfo& transform, WidgetDrawCommands& drawCommands) override;

    TransformInfo ComputeChildTransform(const Shared<ContainerWidgetSlot>& slot, const TransformInfo& myTransform);
    virtual TransformInfo ComputeChildTransform(const Shared<Widget>& widget, const TransformInfo& myTransform);

    EClipMode GetClipMode() const;

    void SetClipMode(EClipMode clipMode);
};

template <typename T, typename... TArgs>
std::enable_if_t<std::is_constructible_v<T, TArgs...> && std::is_base_of_v<Widget, T>, Shared<ContainerWidgetSlot>>
ContainerWidget::AddChild(TArgs&&... args)
{
    return AddChild(newShared<T>(std::forward<TArgs>(args)...));
}

template <typename TSlotType, typename T, typename... TArgs>
std::enable_if_t<std::is_constructible_v<T, TArgs...> && std::is_base_of_v<Widget, T> && std::is_base_of_v<
                     ContainerWidgetSlot, TSlotType>, Shared<TSlotType>> ContainerWidget::AddChild(TArgs&&... args)
{
    if (auto slot = AddChild<T>(std::forward<TArgs>(args)...))
    {
        return slot->template As<TSlotType>();
    }
    return Shared<TSlotType>{};
}


template <typename T, typename = std::enable_if_t<std::is_base_of_v<ContainerWidget, T>>>
std::pair<Shared<typename T::SlotType>, Shared<T>> operator+(const Shared<T>& container, const Shared<Widget>& other)
{
    auto slot = container->AddChild(other);
    return std::make_pair(slot->template As<T::template SlotType>(), container);
}


template <typename T, typename = std::enable_if_t<std::is_base_of_v<ContainerWidget, T>>>
std::pair<Shared<typename T::SlotType>, Shared<T>> operator+(
    const std::pair<Shared<typename T::SlotType>, Shared<T>>& container, const Shared<Widget>& other)
{
    auto slot = container.second->AddChild(other);
    return std::make_pair(slot->template As<T::template SlotType>(), container.second);
}

template <typename T, typename E, typename = std::enable_if_t<std::is_base_of_v<ContainerWidget, T> && std::is_base_of_v
              <ContainerWidget, E>>>
std::pair<Shared<typename T::SlotType>, Shared<T>> operator+(const Shared<T>& container,
                                                             const std::pair<Shared<typename E::SlotType>, Shared<E>>&
                                                             other)
{
    auto slot = container->AddChild(other.second);
    return std::make_pair(slot->template As<T::template SlotType>(), container.second);
}

template <typename T, typename = std::enable_if_t<std::is_base_of_v<ContainerWidget, T>>>
Shared<T> operator-(const Shared<T>& container, const Shared<Widget>& child)
{
    container->RemoveChild(child);
    return container;
}
