#pragma once
#include "aerox/core/Disposable.hpp"
#include "aerox/core/math/Matrix3.hpp"
#include "aerox/core/math/Matrix4.hpp"
#include "aerox/core/math/Vec2.hpp"
#include "aerox/core/math/Vec4.hpp"
#include "aerox/graphics/Frame.hpp"
#include "aerox/graphics/shaders/GraphicsShader.hpp"

namespace aerox::widgets
{
    struct QuadInfo
    {
        int textureId = -1;
        int samplerId = -1;
        Vec4<float> color{0.0f};
        Vec4<float> borderRadius{0.0f};
        Vec2<float> size{0.0f};
        glm::mat3 transform{};
    };

    struct BatchInfo {
        Vec4<float> viewport;
        Matrix4<float> projection;
        QuadInfo quads[1024];
    };
    
    class BatchRenderer : public Disposable
    {
        std::map<uint32_t,vk::DescriptorSetLayout> _layout{};
    public:
        Shared<graphics::GraphicsShader> batchShader{};
        BatchRenderer();
        void Draw(graphics::Frame * frame,const std::vector<QuadInfo>& batches);
    };
}
