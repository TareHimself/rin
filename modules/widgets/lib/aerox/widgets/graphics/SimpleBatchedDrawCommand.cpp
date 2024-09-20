#include "aerox/widgets/graphics/SimpleBatchedDrawCommand.hpp"


namespace aerox::widgets
{
    SimpleBatchedDrawCommand::Builder& SimpleBatchedDrawCommand::Builder::AddRect(Vec2<float> size,
        Matrix3<float> transform, Vec4<float> borderRadius, Vec4<float> color)
    {
        quads.emplace_back(-1,color,borderRadius,size,transform);
        return *this;
    }

    SimpleBatchedDrawCommand::Builder& SimpleBatchedDrawCommand::Builder::AddTexture(int textureId, Vec2<float> size,
        Matrix3<float> transform, Vec4<float> borderRadius, Vec4<float> color)
    {
        quads.emplace_back(textureId,color,borderRadius,size,transform);
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
}
