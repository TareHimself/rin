#include "aerox/widgets/BatchRenderer.hpp"

#include "aerox/core/utils.hpp"

namespace aerox::widgets
{
    BatchRenderer::BatchRenderer()
    {
        batchShader = graphics::GraphicsShader::FromFile(getResourcesPath() / "shaders" / "batch.ash");
        _layout = batchShader->ComputeDescriptorSetLayouts();
    }

    void BatchRenderer::Draw(graphics::Frame* frame, const std::vector<QuadInfo>& batches)
    {
        
    }
}
