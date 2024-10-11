#pragma once
#include "rin/core/math/Vec2.hpp"
#include "rin/widgets/ContainerWidgetSlot.hpp"


class PanelWidget;

class PanelWidgetSlot : public ContainerWidgetSlot
{
public:
    PanelWidgetSlot(PanelWidget* panel, const Shared<Widget>& widget);


    Vec2<float> alignment{0.0f};
    Vec2<float> maxAnchor{0.0f};
    Vec2<float> minAnchor{0.0f};
    Vec2<float> offset{0.0f};
    Vec2<float> size{0.0f};
    bool sizeToContent{false};
};
