#pragma once
#include "aerox/core/math/Vec2.hpp"
#include "aerox/widgets/ContainerSlot.hpp"

namespace aerox::widgets
{
    class Panel;
}

namespace aerox::widgets
{
    class PanelSlot : public ContainerSlot
    {
        Panel * _panel{};
    public:
        PanelSlot(Panel * panel,const Shared<Widget>& widget);
        
        static bool NearlyEqual(double a,double b, double tolerance = 0.001);
        
        Vec2<float> alignment{0.0f};
        Vec2<float> maxAnchor{0.0f};
        Vec2<float> minAnchor{0.0f};
        Vec2<float> offset{0.0f};
        Vec2<float> size{0.0f};
        bool sizeToContent{false};
        
        void ComputeSizeAndOffset() const;
    };
}
