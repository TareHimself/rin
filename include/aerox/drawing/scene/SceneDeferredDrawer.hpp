#pragma once
#include "SceneDrawer.hpp"

namespace aerox::drawing {
struct GBuffer {
  std::shared_ptr<AllocatedImage> location;
  std::shared_ptr<AllocatedImage> color;
  std::shared_ptr<AllocatedImage> normals;
  std::shared_ptr<AllocatedImage> roughnessMetallic;
  std::shared_ptr<AllocatedImage> emissive;
  std::shared_ptr<AllocatedImage> specular;

  void Clear();
};
class SceneDeferredDrawer : public SceneDrawer {
  SceneGlobalBuffer _sceneData{};
  std::shared_ptr<AllocatedBuffer> _sceneGlobalBuffer;
  std::shared_ptr<MaterialInstance> _defaultCheckeredMaterial;
  
  GBuffer _gBuffer{};
  std::shared_ptr<AllocatedImage> _depth;
  std::shared_ptr<AllocatedImage> _result;
  std::shared_ptr<MaterialInstance> _shader;
  vk::Sampler _sampler;
public:

  void OnInit(scene::Scene * owner) override;
  void CreateBuffers();

  std::shared_ptr<AllocatedImage> CreateBufferImage();
  std::shared_ptr<AllocatedImage> CreateDepthImage();
  std::shared_ptr<AllocatedImage> CreateRenderTargetImage();

  void Draw(RawFrameData *frameData) override;

  void TransitionGBuffer(vk::CommandBuffer cmd, vk::ImageLayout from, vk::ImageLayout to);

  Array<vk::RenderingAttachmentInfo> MakeAttachments();

  Array<vk::Format> GetColorAttachmentFormats() override;

  std::weak_ptr<AllocatedImage> GetRenderTarget() override;

  std::shared_ptr<MaterialInstance> CreateMaterialInstance(const Array<std::shared_ptr<Shader>> &shaders) override;
  
  std::weak_ptr<MaterialInstance> GetDefaultMaterial() override;

  void OnDestroy() override;
};
}
