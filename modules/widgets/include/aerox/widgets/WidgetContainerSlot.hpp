#pragma once
#include "aerox/core/Disposable.hpp"

class Widget;

class WidgetContainerSlot : public Disposable
{
    Shared<Widget> _widget{};
public:
    Shared<Widget> GetWidget() const;

    template<typename T,typename = std::enable_if_t<std::is_base_of_v<WidgetContainerSlot,T>,T>>
    Shared<T> As();

    WidgetContainerSlot(const Shared<Widget>& widget);
};

template <typename T,typename>
Shared<T> WidgetContainerSlot::As()
{
    return  this->GetSharedDynamic<T>();
}
