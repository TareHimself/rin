#pragma once
#include "aerox/widgets/Container.hpp"

namespace aerox::widgets
{

    enum class FitMode
    {
        None,
        Fill,
        Contain,
        Cover
    };

    
    class Fitter : public Container
    {
        FitMode _mode = FitMode::None;
    public:
        FitMode GetMode() const;
        void SetMode(FitMode mode);
        Vec2<float> ComputeDesiredSize() override;
        size_t GetMaxSlots() const override;
        void ArrangeSlots(const Vec2<float>& drawSize) override;

        static Vec2<float> ComputeContainSize(const Vec2<float>& drawSize,const Vec2<float>& widgetSize);
        static Vec2<float> ComputeCoverSize(const Vec2<float>& drawSize,const Vec2<float>& widgetSize);
        void SizeContent(const Vec2<float>& size) const;
    };
}
