#pragma once
#include "aerox/core/Disposable.hpp"
#include "aerox/core/math/Matrix3.hpp"
#include "aerox/core/math/Vec2.hpp"
#include "aerox/core/math/Vec4.hpp"
#include "aerox/graphics/shaders/GraphicsShader.hpp"

namespace aerox::widgets
{
    struct QuadRenderInfo
    {
        int textureId = -1;
        int samplerId = -1;
        Vec4<float> color{0.0f};
        Vec4<float> borderRadius{0.0f};
        Vec2<float> size{0.0f};
        glm::mat3 transform{};
    };
    
    class BatchQuadRenderer : public Disposable
    {
    public:
        Shared<graphics::GraphicsShader> batchShader{};
        BatchQuadRenderer();
        void Test();
    };
}
