#pragma once
#include "aerox/core/Disposable.hpp"

namespace aerox::widgets
{
    class Widget;

    class ContainerSlot : public Disposable
    {
        Shared<Widget> _widget{};
    public:
        Shared<Widget> GetWidget() const;

        template<typename T,typename = std::enable_if_t<std::is_base_of_v<ContainerSlot,T>,T>>
        Shared<T> As();

        ContainerSlot(const Shared<Widget>& widget);
    };

    template <typename T,typename>
    Shared<T> ContainerSlot::As()
    {
        return  this->GetSharedDynamic<T>();
    }
}
