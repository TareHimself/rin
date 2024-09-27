#pragma once
#include "rin/core/math/Vec2.hpp"
#include "rin/widgets/WidgetContainerSlot.hpp"


class WidgetPanel;
    class WidgetPanelSlot : public WidgetContainerSlot
    {
        WidgetPanel * _panel{};
    public:
        WidgetPanelSlot(WidgetPanel * panel,const Shared<Widget>& widget);
        
        static bool NearlyEqual(double a,double b, double tolerance = 0.001);
        
        Vec2<float> alignment{0.0f};
        Vec2<float> maxAnchor{0.0f};
        Vec2<float> minAnchor{0.0f};
        Vec2<float> offset{0.0f};
        Vec2<float> size{0.0f};
        bool sizeToContent{false};
        
        void ComputeSizeAndOffset() const;
    };
