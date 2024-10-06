#pragma once
#include "WidgetBatchedDrawCommand.hpp"

class SimpleBatchedDrawCommand : public WidgetBatchedDrawCommand
{
    std::vector<QuadInfo> _quads{};
public:
    struct Builder
    {
        std::vector<QuadInfo> quads{};
        Builder& AddRect(Vec2<float> size,Matrix3<float> transform,
        Vec4<float> borderRadius = Vec4{0.0f},
        Vec4<float> color = Vec4{1.0f});
        
        Builder& AddTexture(int textureId,const Vec2<float>& size,const Matrix3<float>& transform,
        const Vec4<float>& borderRadius = Vec4{0.0f},
        const Vec4<float>& color = Vec4{1.0f},const Vec4<float>& uv = Vec4{0.0f,0.0f,1.0f,1.0f});
        Builder& AddMtsdf(int textureId,const Vec2<float>& size,const Matrix3<float>& transform,
        const Vec4<float>& borderRadius = Vec4{0.0f},
        const Vec4<float>& color = Vec4{1.0f},const Vec4<float>& uv = Vec4{0.0f,0.0f,1.0f,1.0f});
        Shared<SimpleBatchedDrawCommand> Finish();
    };
    SimpleBatchedDrawCommand(const std::vector<QuadInfo>& quads);
    std::vector<QuadInfo> ComputeQuads() const override;
};
