#pragma once
#include "BatchedDrawCommand.hpp"
#include "DrawCommand.hpp"
#include "QuadInfo.hpp"
#include "aerox/core/math/Matrix3.hpp"

namespace aerox::widgets
{
    
    
    class RectBatchedDrawCommand : public BatchedDrawCommand
    {
    public:
        struct Rect
        {
            Matrix3<float> transform{1.0};
            Vec2<float> size{0.0f};
            Vec4<float> borderRadius{0.0f};
            Vec4<float> color{0.0f};
        };
    private:
        std::vector<Rect> _rects{};
    public:
        RectBatchedDrawCommand(const std::vector<Rect>& rects);
        std::vector<QuadInfo> ComputeQuads() const override;
        static Shared<RectBatchedDrawCommand> New(const std::vector<Rect>& rects);
    };
}
