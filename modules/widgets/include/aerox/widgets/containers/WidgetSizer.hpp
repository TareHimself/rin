#pragma once
#include "aerox/widgets/WidgetContainer.hpp"

class WidgetSizer : public WidgetContainer
{
    std::optional<float> _widthOverride{};
    std::optional<float> _heightOverride{};
public:
    void SetWidthOverride(const std::optional<float>& width);
    void SetHeightOverride(const std::optional<float>& height);
        
    Vec2<float> ComputeDesiredSize() override;
    size_t GetMaxSlots() const override;
    void ArrangeSlots(const Vec2<float>& drawSize) override;

    
};
