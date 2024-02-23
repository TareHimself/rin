#pragma once
#include "SceneDrawer.hpp"

namespace vengine::drawing {
struct GBuffer {
  Managed<AllocatedImage> location;
  Managed<AllocatedImage> color;
  Managed<AllocatedImage> normals;
  Managed<AllocatedImage> roughnessMetallic;
  Managed<AllocatedImage> emissive;
  Managed<AllocatedImage> specular;
};
class SceneDeferredDrawer : public SceneDrawer {
  GBuffer _gBuffer{};
  Managed<AllocatedImage> _depth;
  Managed<AllocatedImage> _result;
  Managed<MaterialInstance> _shader;
  vk::Sampler _sampler;
public:

  void Init(scene::Scene *outer) override;
  void CreateBuffers();

  Managed<AllocatedImage> CreateBufferImage();
  Managed<AllocatedImage> CreateDepthImage();
  Managed<AllocatedImage> CreateRenderTargetImage();

  void Draw(RawFrameData *frameData) override;

  void TransitionGBuffer(vk::CommandBuffer cmd, vk::ImageLayout from, vk::ImageLayout to);

  Array<vk::RenderingAttachmentInfo> MakeAttachments();

  Array<vk::Format> GetColorAttachmentFormats() override;

  Ref<AllocatedImage> GetRenderTarget() override;
};
}
