#pragma once
#include "BatchedDrawCommand.hpp"

namespace aerox::widgets
{
    class SimpleBatchedDrawCommand : public BatchedDrawCommand
    {
        std::vector<QuadInfo> _quads{};
    public:
        struct Builder
        {
            std::vector<QuadInfo> quads{};
            Builder& AddRect(Vec2<float> size,Matrix3<float> transform,
            Vec4<float> borderRadius = Vec4{0.0f},
            Vec4<float> color = Vec4{1.0f});
            Builder& AddTexture(int textureId,Vec2<float> size,Matrix3<float> transform,
            Vec4<float> borderRadius = Vec4{0.0f},
            Vec4<float> color = Vec4{1.0f});
            Shared<SimpleBatchedDrawCommand> Finish();
        };
        SimpleBatchedDrawCommand(const std::vector<QuadInfo>& quads);
        std::vector<QuadInfo> ComputeQuads() const override;
    };
}
