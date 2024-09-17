#pragma once
#include "aerox/widgets/Container.hpp"


namespace aerox::widgets
{
    class Panel : public Container
    {
    protected:
        Vec2<float> ComputeDesiredSize() override;
        void ArrangeSlots(const Vec2<float>& drawSize) override;
        Shared<ContainerSlot> MakeSlot(const Shared<Widget>& widget) override;
        void OnChildResized(Widget* widget) override;
    public:
    };
}
