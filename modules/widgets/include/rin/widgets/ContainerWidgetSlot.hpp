#pragma once
#include "rin/core/Disposable.hpp"

class ContainerWidget;
class Widget;

class ContainerWidgetSlot : public Disposable
{
    Shared<ContainerWidget> _owner{};
    Shared<Widget> _widget{};

protected:
    ContainerWidget * GetContainer() const;
public:
    Shared<Widget> GetWidget() const;

    template<typename T,typename = std::enable_if_t<std::is_base_of_v<ContainerWidgetSlot,T>,T>>
    Shared<T> As();
    
    ContainerWidgetSlot(ContainerWidget * owner,const Shared<Widget>& widget);

    virtual void Update();
};

template <typename T,typename>
Shared<T> ContainerWidgetSlot::As()
{
    return  this->GetSharedDynamic<T>();
}
