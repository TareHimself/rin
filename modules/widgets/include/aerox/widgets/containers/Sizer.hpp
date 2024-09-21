#pragma once
#include "aerox/widgets/Container.hpp"

namespace aerox::widgets
{
    class Sizer : public Container
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
}
