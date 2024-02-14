#pragma once

#include "Shader.hpp"
#include "ShaderManager.hpp"
#include "descriptors.hpp"
#include "types.hpp"
#include "vengine/EngineSubsystem.hpp"
#include "vengine/Object.hpp"
#include "vengine/containers/Array.hpp"
#include "vengine/containers/TDispatcher.hpp"
#include <vulkan/vulkan.hpp>
#include "generated/drawing/DrawingSubsystem.reflect.hpp"


namespace vengine::drawing {
class Texture2D;
class Allocator;
class MaterialInstance;
class Mesh;
}

namespace vengine {
class Engine;
}

namespace vengine::scene {
class Scene;
class CameraComponent;
}

// namespace vengine

namespace vengine {
namespace drawing {

class Viewport;


constexpr unsigned int FRAME_OVERLAP = 2;


/**
 * \brief Base class for the engine renderer. uses vulkan
 */

RCLASS()
class DrawingSubsystem : public EngineSubsystem {


  long long _frameCount = 0;

  vk::Instance _instance = nullptr;

  std::mutex _queueMutex;
  std::mutex _deviceMutex;
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
  Managed<AllocatedImage> _drawImage;
  Managed<AllocatedImage> _depthImage;

  // Default Images
  Managed<Texture2D> _whiteTexture;
  Managed<Texture2D> _blackTexture;
  Managed<Texture2D> _greyTexture;
  Managed<Texture2D> _errorCheckerboardTexture;

  // Default Samplers
  vk::Sampler _defaultSamplerLinear;
  vk::Sampler _defaultSamplerNearest;
  
  DescriptorAllocatorGrowable _globalAllocator{};
  Ref<DescriptorSet> _drawImageDescriptors;
  vk::DescriptorSetLayout _drawImageDescriptorLayout;

  vk::Pipeline _computePipeline;
  vk::PipelineLayout _computePipelineLayout;

  vk::Fence _immediateFence;
  vk::CommandBuffer _immediateCommandBuffer;
  vk::CommandPool _immCommandPool;


  vk::Pipeline _mainPipeline;
  vk::PipelineLayout _mainPipelineLayout;
  Managed<Allocator> _allocator;
  RawFrameData _frames[FRAME_OVERLAP];

  bool _bResizeRequested = false;
  bool _bIsResizingSwapchain = false;

  Managed<ShaderManager> _shaderManager;
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

  void DrawBackground(RawFrameData *frame) const;

  void DrawScenes(RawFrameData *frame);

  void DrawUI(RawFrameData *frame);

  

  static vk::RenderingInfo MakeRenderingInfo(vk::Extent2D drawExtent);
  static void GenerateMipMaps(vk::CommandBuffer cmd, vk::Image image,
                              vk::Extent2D size, const vk::Filter &filter);
  
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

  static uint32_t CalcMipLevels(const vk::Extent2D & extent);
public:
  String GetName() const override;
  float renderScale = 1.f;

  Array<ComputeEffect> backgroundEffects;

  int currentBackgroundEffect{0};
  
  RawFrameData *GetCurrentFrame();

  void WaitDeviceIdle();

  vk::Extent2D GetSwapchainExtent() const;

  vk::Extent2D GetSwapchainExtentScaled() const;
  
  vk::Extent2D GetDrawImageExtent() const;
  vk::Extent2D GetDrawImageExtentScaled() const;
  vk::Format GetDrawImageFormat() const;

  vk::Format GetDepthImageFormat() const;
  vk::Device GetDevice() const;
  vk::PhysicalDevice GetPhysicalDevice() const;
  vk::Instance GetVulkanInstance() const;
  Ref<Allocator> GetAllocator() const;


  Ref<Texture2D> GetDefaultWhiteTexture() const;
  Ref<Texture2D> GetDefaultBlackTexture() const;
  Ref<Texture2D> GetDefaultGreyTexture() const;
  Ref<Texture2D> GetDefaultErrorCheckerboardTexture() const;

  //void onResize(const std::function<void()> & callback);
  // Default Samplers
  vk::Sampler GetDefaultSamplerLinear() const;
  vk::Sampler GetDefaultSamplerNearest() const;

  Ref<ShaderManager> GetShaderManager() const;
  
  DescriptorAllocatorGrowable * GetGlobalDescriptorAllocator();
  
  bool ResizePending() const;

  void RequestResize();

  bool IsResizingSwapchain() const;

  Managed<AllocatedImage> CreateImage(vk::Extent3D size,
                                      vk::Format format,
                                      vk::ImageUsageFlags usage,
                                      bool mipMapped = false) const;
  Managed<AllocatedImage> CreateImage(const void *data,
                                      vk::Extent3D size,
                                      vk::Format format,
                                      vk::ImageUsageFlags usage,
                                      bool mipMapped = false,const vk::Filter& mipMapFilter = vk::Filter::eLinear);
  
  
  void ResizeSwapchain();

  void CreateComputeShader(const Shader *shader, ComputeEffect &effect);

  void Init(Engine * outer) override;

  void BeforeDestroy() override;

  void ImmediateSubmit(std::function<void(vk::CommandBuffer cmd)> &&function);

  //void drawImGui(vk::CommandBuffer cmd, vk::ImageView view);

  virtual void Draw();

  TDispatcher<vk::Extent2D> onResizeEvent;

  Managed<GpuMeshBuffers> CreateMeshBuffers(const Mesh *mesh);
};

REFLECT_IMPLEMENT(DrawingSubsystem)
} // namespace rendering
} // namespace vengine
