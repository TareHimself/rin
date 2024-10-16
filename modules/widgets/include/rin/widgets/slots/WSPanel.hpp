#pragma once
#include "rin/core/math/Vec2.hpp"
#include "rin/widgets/ContainerWidgetSlot.hpp"


class WCPanel;

class WSPanel : public ContainerWidgetSlot
{
public:
    WSPanel(WCPanel* panel, const Shared<Widget>& widget);


    Vec2<float> alignment{0.0f};
    Vec2<float> maxAnchor{0.0f};
    Vec2<float> minAnchor{0.0f};
    Vec2<float> offset{0.0f};
    Vec2<float> size{0.0f};
    bool sizeToContent{false};
};
