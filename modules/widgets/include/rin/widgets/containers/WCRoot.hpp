#pragma once
#include "rin/widgets/ContainerWidget.hpp"


class WCRoot : public ContainerWidget
{
public:

    Vec2<float> ComputeContentSize() override;
    void ArrangeSlots(const Vec2<float>& drawSize) override;
};
