#pragma once

#include "Shader.hpp"
#include "ShaderManager.hpp"
#include "descriptors.hpp"
#include "types.hpp"
#include "vengine/EngineSubsystem.hpp"
#include "vengine/Object.hpp"
#include "vengine/containers/Array.hpp"
#include "vengine/containers/TEventDispatcher.hpp"
#include <vk_mem_alloc.h>
#include <filesystem>
#include <vulkan/vulkan.hpp>

namespace vengine {
namespace drawing {
class Allocator;
}
}

namespace vengine::drawing {
class Texture;
}

namespace vengine::drawing {
class MaterialInstance;
}

namespace vengine::drawing {
class Mesh;
}

namespace vengine::scene {
class CameraComponent;
}

namespace vengine {
class Engine;
}

namespace vengine::scene {
class Scene;
}

// namespace vengine

namespace vengine {
namespace drawing {

class Viewport;


constexpr unsigned int FRAME_OVERLAP = 2;


/**
 * \brief Base class for the engine renderer. uses vulkan
 */
class Drawer : public EngineSubsystem {


  long long _frameCount = 0;

  vk::Instance _instance = nullptr;

#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
  vk::DebugUtilsMessengerEXT _debugMessenger = nullptr;
#endif

  vk::PhysicalDevice _gpu = nullptr;
  vk::Device _device = nullptr;
  vk::SurfaceKHR _surface = nullptr;
  vk::SwapchainKHR _swapchain = nullptr;
  vk::Format _swapchainImageFormat = vk::Format::eUndefined;
  Array<vk::Image> _swapchainImages;
  Array<vk::ImageView> _swapchainImageViews;
  vk::Queue _graphicsQueue = nullptr;
  uint32_t _graphicsQueueFamily = -1;

  // Images
  AllocatedImage _drawImage{};
  AllocatedImage _depthImage{};

  // Default Images
  Texture * _whiteTexture = nullptr;
  Texture * _blackTexture = nullptr;
  Texture * _greyTexture = nullptr;
  Texture * _errorCheckerboardTexture = nullptr;

  // Default Samplers
  vk::Sampler _defaultSamplerLinear;
  vk::Sampler _defaultSamplerNearest;

  MaterialInstance * _defaultCheckeredMaterial = nullptr;
  

  //vk::RenderPass renderPass = nullptr;
  //Array<vk::Framebuffer> frameBuffers;

  DescriptorAllocatorGrowable _globalAllocator;
  vk::DescriptorSet _drawImageDescriptors;
  vk::DescriptorSetLayout _drawImageDescriptorLayout;

  vk::Pipeline _computePipeline;
  vk::PipelineLayout _computePipelineLayout;

  vk::Fence _immediateFence;
  vk::CommandBuffer _immediateCommandBuffer;
  vk::CommandPool _immCommandPool;


  vk::Pipeline _mainPipeline;
  vk::PipelineLayout _mainPipelineLayout;
  Allocator * _Allocator = nullptr;
  RawFrameData _frames[FRAME_OVERLAP];

  VkDescriptorSetLayout _sceneDescriptorSetLayout;

  bool _resizePending = false;

  ShaderManager *_shaderManager = nullptr;
  Array<std::function<void()>> _resizeCallbacks;
protected:

  void InitSwapchain();
  
  void CreateSwapchain();

  void DestroySwapchain();
  
  void InitCommands();

  void InitSyncStructures();

  void InitPipelineLayout();

  void InitPipelines();

  void InitDescriptors();

  //void initImGui();

  void InitDefaultTextures();

  void InitDefaultMaterials();

  RawFrameData *GetCurrentFrame();

  void DrawBackground(RawFrameData *frame) const;

  void DrawScenes(RawFrameData *frame);

  void DrawUI(RawFrameData *frame);

  

  static vk::RenderingInfo MakeRenderingInfo(vk::Extent2D drawExtent);
  static void TransitionImage(vk::CommandBuffer cmd, vk::Image image,
                              vk::ImageLayout currentLayout,
                              vk::ImageLayout newLayout);
  static vk::ImageSubresourceRange ImageSubResourceRange(
      vk::ImageAspectFlags aspectMask);
  static void CopyImageToImage(vk::CommandBuffer cmd, vk::Image src,
                               vk::Image dst, vk::Extent2D srcSize,
                               vk::Extent2D dstSize);

  static vk::ImageCreateInfo MakeImageCreateInfo(vk::Format format,vk::Extent3D size,vk::ImageUsageFlags usage);

  static vk::ImageViewCreateInfo MakeImageViewCreateInfo(vk::Format format,vk::Image image,vk::ImageAspectFlags aspect);
  
  static vk::RenderingAttachmentInfo MakeRenderingAttachment(
      vk::ImageView view,
      vk::ImageLayout layout = vk::ImageLayout::eAttachmentOptimal,
      const std::optional<vk::ClearValue> &clear = std::nullopt);
public:

  String GetName() const override;
  float renderScale = 1.f;

  Array<ComputeEffect> backgroundEffects;

  int currentBackgroundEffect{0};

  vk::Extent2D GetSwapchainExtent() const;

  vk::Extent2D GetSwapchainExtentScaled() const;

  vk::Extent2D GetDrawImageExtent() const;
  vk::Format GetDrawImageFormat() const;

  vk::Format GetDepthImageFormat() const;
  
  Engine * GetEngine() const;
  vk::Device GetDevice() const;
  vk::PhysicalDevice GetPhysicalDevice() const;
  vk::Instance GetVulkanInstance() const;
  Allocator *GetAllocator() const;


  Texture * GetDefaultWhiteTexture() const;
  Texture * GetDefaultBlackTexture() const;
  Texture * GetDefaultGreyTexture() const;
  Texture * GetDefaultErrorCheckerboardTexture() const;

  //void onResize(const std::function<void()> & callback);
  // Default Samplers
  vk::Sampler GetDefaultSamplerLinear() const;
  vk::Sampler GetDefaultSamplerNearest() const;

  MaterialInstance * GetDefaultCheckeredMaterial() const;

  ShaderManager * GetShaderManager() const;

  vk::DescriptorSetLayout GetSceneDescriptorLayout() const;

  DescriptorAllocatorGrowable * GetGlobalDescriptorAllocator();
  
  bool ResizePending() const;

  void RequestResize();

  AllocatedImage CreateImage(vk::Extent3D size,vk::Format format,vk::ImageUsageFlags usage,bool mipMapped = false) const;
  AllocatedImage CreateImage(const void* data,vk::Extent3D size,vk::Format format,vk::ImageUsageFlags usage,bool mipMapped = false);
  
  
  void ResizeSwapchain();

  void CreateComputeShader(const Shader *shader, ComputeEffect &effect);

  void Init(Engine *outer) override;

  void HandleDestroy() override;

  void ImmediateSubmit(std::function<void(vk::CommandBuffer cmd)> &&function);

  //void drawImGui(vk::CommandBuffer cmd, vk::ImageView view);

  virtual void Draw();

  TEventDispatcher<vk::Extent2D> onResizeEvent;

  GpuMeshBuffers CreateMeshBuffers(const Mesh *mesh);
};
} // namespace rendering
} // namespace vengine
