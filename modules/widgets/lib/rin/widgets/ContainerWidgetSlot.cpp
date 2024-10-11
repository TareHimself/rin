#include "rin/widgets/ContainerWidgetSlot.hpp"

#include "rin/widgets/ContainerWidget.hpp"

ContainerWidget* ContainerWidgetSlot::GetContainer() const
{
    return _owner.get();
}

Shared<Widget> ContainerWidgetSlot::GetWidget() const
{
    return _widget;
}

ContainerWidgetSlot::ContainerWidgetSlot(ContainerWidget* owner, const Shared<Widget>& widget)
{
    _owner = owner->GetSharedDynamic<ContainerWidget>();
    _widget = widget;
}

void ContainerWidgetSlot::Update()
{
    if (auto container = GetContainer())
    {
        if (container->HasSurface())
        {
            container->OnChildSlotUpdated(this);
        }
    }
}
