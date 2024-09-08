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

    public:
        size_t GetMaxSlots() const override;
    };
}
