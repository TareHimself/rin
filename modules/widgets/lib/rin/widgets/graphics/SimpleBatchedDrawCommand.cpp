#include "rin/widgets/graphics/SimpleBatchedDrawCommand.hpp"


SimpleBatchedDrawCommand::Builder& SimpleBatchedDrawCommand::Builder::AddRect(Vec2<float> size,
        Matrix3<float> transform, Vec4<float> borderRadius, Vec4<float> color)
{
    quads.emplace_back(Vec4{-1,0,0,0},color,borderRadius,size,transform);
    return *this;
}

SimpleBatchedDrawCommand::Builder& SimpleBatchedDrawCommand::Builder::AddTexture(int textureId, const Vec2<float>& size,
    const Matrix3<float>& transform, const Vec4<float>& borderRadius, const Vec4<float>& color, const Vec4<float>& uv)
{
    quads.emplace_back(Vec4{textureId,0,0,0},color,borderRadius,size,transform,uv);
    return *this;
}

SimpleBatchedDrawCommand::Builder& SimpleBatchedDrawCommand::Builder::AddMtsdf(int textureId, const Vec2<float>& size,
    const Matrix3<float>& transform, const Vec4<float>& borderRadius, const Vec4<float>& color, const Vec4<float>& uv)
{
    quads.emplace_back(Vec4{textureId,1,0,0},color,borderRadius,size,transform,uv);
    return *this;
}

Shared<SimpleBatchedDrawCommand> SimpleBatchedDrawCommand::Builder::Finish()
{
    return newShared<SimpleBatchedDrawCommand>(quads);
}

SimpleBatchedDrawCommand::SimpleBatchedDrawCommand(const std::vector<QuadInfo>& quads)
{
    _quads = quads;
}

std::vector<QuadInfo> SimpleBatchedDrawCommand::ComputeQuads() const
{
    return _quads;
}
