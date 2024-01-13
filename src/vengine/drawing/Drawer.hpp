#pragma once
#include "Shader.hpp"
#include "ShaderManager.hpp"
#include "descriptors.hpp"
#include "types.hpp"
#include "vengine/Object.hpp"
#include "vengine/containers/Array.hpp"
#include <vk_mem_alloc.hpp>
#include <filesystem>
#include <vulkan/vulkan.hpp>

namespace vengine {
namespace drawing {
class Material;
}
}

namespace vengine {
namespace drawing {
class Mesh;
}
}

namespace vengine {
namespace scene {
class CameraComponent;
}
}

namespace vengine {
class Engine;
}

namespace vengine {
namespace scene {
class Scene;
}
} // namespace vengine

namespace vengine {
namespace drawing {

class Viewport;


constexpr unsigned int FRAME_OVERLAP = 2;


/**
 * \brief Base class for the engine renderer. uses vulkan
 */
class Drawer : public Object<Engine> {


  long long frameCount = 0;

  vk::Instance instance = nullptr;

#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
  vk::DebugUtilsMessengerEXT debugMessenger = nullptr;
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
  AllocatedImage _whiteImage{};
  AllocatedImage _blackImage{};
  AllocatedImage _greyImage{};
  AllocatedImage _errorCheckerboardImage{};

  // Default Samplers
  vk::Sampler _defaultSamplerLinear;
  vk::Sampler _defaultSamplerNearest;

  Material * _defaultCheckeredMaterial = nullptr;
  

  //vk::RenderPass renderPass = nullptr;
  //Array<vk::Framebuffer> frameBuffers;

  DescriptorAllocatorGrowable _descriptorAllocator;
  vk::DescriptorSet _drawImageDescriptors;
  vk::DescriptorSetLayout _drawImageDescriptorLayout;

  vk::Pipeline _computePipeline;
  vk::PipelineLayout _computePipelineLayout;

  vk::Fence _immediateFence;
  vk::CommandBuffer _immediateCommandBuffer;
  vk::CommandPool _immCommandPool;


  vk::Pipeline _mainPipeline;
  vma::Allocator _vkAllocator = nullptr;
  FrameData _frames[FRAME_OVERLAP];

  VkDescriptorSetLayout _sceneDescriptorSetLayout;

  bool _resizePending = false;

  ShaderManager *_shaderManager = nullptr;

protected:

  void initSwapchain();
  
  void createSwapchain();

  void destroySwapchain();
  
  void initCommands();

  void initSyncStructures();

  void initPipelineLayout();

  void initPipelines();

  void initDescriptors();

  void initImGui();

  void initDefaultTextures();

  void initDefaultMaterials();

  FrameData *getCurrentFrame();

  void drawBackground(FrameData *frame);

  void drawScenes(FrameData *frame);

  void allocateImage(AllocatedImage &image, vk::Format format,
                             vk::Extent3D extent, vk::ImageUsageFlags usage,
                             vk::ImageAspectFlags aspectFlags,
                             vma::MemoryUsage memoryUsage = {},
                             vk::MemoryPropertyFlags requiredFlags = {}) const;

  static vk::RenderingInfo makeRenderingInfo(vk::Extent2D drawExtent);
  static void transitionImage(vk::CommandBuffer cmd, vk::Image image,
                              vk::ImageLayout currentLayout,
                              vk::ImageLayout newLayout);
  static vk::ImageSubresourceRange imageSubResourceRange(
      vk::ImageAspectFlags aspectMask);
  static void copyImageToImage(vk::CommandBuffer cmd, vk::Image src,
                               vk::Image dst, vk::Extent2D srcSize,
                               vk::Extent2D dstSize);

  static vk::ImageCreateInfo makeImageCreateInfo(vk::Format format,vk::Extent3D size,vk::ImageUsageFlags usage);

  static vk::ImageViewCreateInfo makeImageViewCreateInfo(vk::Format format,vk::Image image,vk::ImageAspectFlags aspect);
  
  static vk::RenderingAttachmentInfo makeRenderingAttachment(
      vk::ImageView view,
      vk::ImageLayout layout = vk::ImageLayout::eAttachmentOptimal,
      const std::optional<vk::ClearValue> &clear = std::nullopt);

public:
  
  float renderScale = 1.f;

  vk::PipelineLayout _mainPipelineLayout;

  Array<ComputeEffect> backgroundEffects;

  int currentBackgroundEffect{0};

  vk::Extent2D getSwapchainExtent() const;

  vk::Extent2D getSwapchainExtentScaled() const;

  vk::Extent2D getDrawImageExtent() const;
  vk::Format getDrawImageFormat() const;

  vk::Format getDepthImageFormat() const;
  
  Engine *getEngine() const;
  vk::Device getDevice() const;
  vma::Allocator getAllocator() const;


  AllocatedImage getDefaultWhiteImage() const;
  AllocatedImage getDefaultBlackImage() const;
  AllocatedImage getDefaultGreyImage() const;
  AllocatedImage getDefaultErrorCheckerboardImage() const;

  // Default Samplers
  vk::Sampler getDefaultSamplerLinear() const;
  vk::Sampler getDefaultSamplerNearest() const;

  Material * getDefaultCheckeredMaterial() const;

  ShaderManager * getShaderManager() const;

  vk::DescriptorSetLayout getSceneDescriptorLayout() const;
  
  bool resizePending() const;

  AllocatedBuffer createBuffer(size_t allocSize, vk::BufferUsageFlags usage,
                               vma::MemoryUsage memoryUsage,vk::MemoryPropertyFlags requiredFlags = {},vma::AllocationCreateFlags flags = {});

  AllocatedBuffer createTransferCpuGpuBuffer(size_t size,bool randomAccess);

  AllocatedBuffer createUniformCpuGpuBuffer(size_t size,bool randomAccess);

  void destroyBuffer(const AllocatedBuffer &buffer);

  AllocatedImage createImage(vk::Extent3D size,vk::Format format,vk::ImageUsageFlags usage,bool mipMapped = false);
  AllocatedImage createImage(void* data,vk::Extent3D size,vk::Format format,vk::ImageUsageFlags usage,bool mipMapped = false);
  void destroyImage(const AllocatedImage& image);
  
  void resizeSwapchain();

  void createComputeShader(const Shader *shader, ComputeEffect &effect);

  void init(Engine *outer) override;

  void handleCleanup() override;

  void immediateSubmit(std::function<void(vk::CommandBuffer cmd)> &&function);

  void drawImGui(vk::CommandBuffer cmd, vk::ImageView view);

  virtual void draw();

  GpuMeshBuffers createMeshBuffers(const Mesh *mesh);
};
} // namespace rendering
} // namespace vengine
