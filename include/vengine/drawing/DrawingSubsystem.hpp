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

#include <future>
#include <queue>


namespace vengine {
namespace window {
class Window;
}
}

namespace vengine {
namespace drawing {
class WindowDrawer;
}
}

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

struct GraphicsQueueOp {
  std::function<void(const vk::Queue&)> func;
  std::promise<std::optional<std::exception_ptr>> * pending;
};

/**
 * \brief Base class for the engine renderer. uses vulkan
 */

RCLASS()
class DrawingSubsystem : public EngineSubsystem {

  vk::Instance _instance = nullptr;
  std::mutex _queueMutex;
  std::mutex _deviceMutex;
#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
  vk::DebugUtilsMessengerEXT _debugMessenger = nullptr;
#endif

  vk::PhysicalDevice _gpu = nullptr;
  vk::Device _device = nullptr;
  vk::SurfaceKHR _surface = nullptr;
  vk::Queue _graphicsQueue = nullptr;
  uint32_t _graphicsQueueFamily = -1;

  // Default Images
  Managed<Texture2D> _whiteTexture;
  Managed<Texture2D> _blackTexture;
  Managed<Texture2D> _greyTexture;
  Managed<Texture2D> _errorCheckerboardTexture;

  // Default Samplers
  vk::Sampler _defaultSamplerLinear;
  vk::Sampler _defaultSamplerNearest;
  
  DescriptorAllocatorGrowable _globalAllocator{};
  
  vk::Fence _immediateFence;
  vk::CommandBuffer _immediateCommandBuffer;
  vk::CommandPool _immediateCommandPool;
  
  Managed<Allocator> _allocator;
  
  Managed<ShaderManager> _shaderManager;
  Array<std::function<void()>> _resizeCallbacks;

  std::unordered_map<uint64_t,Managed<WindowDrawer>> _windowDrawers;
  std::queue<GraphicsQueueOp> _submitQueue{};
  std::thread _submitThread;
  std::condition_variable _submitCond;
  std::mutex _submitMutex;
  
protected:

  void InitCommands();

  void InitSyncStructures();

  void InitDescriptors();

  //void initImGui();

  void InitDefaultTextures();

  // void DrawScenes(RawFrameData *frame);
  //
  // void DrawUI(RawFrameData *frame);

  Ref<WindowDrawer> CreateWindowDrawer(const Ref<window::Window>& window);

  
public:

  vk::Format GetSwapchainFormat();
  void SubmitThreadSafe(const vk::SubmitInfo2 &info, const vk::Fence &fence);
  
  void RunQueueOperation(const std::function<void(const vk::Queue &)> &func);
  
  void SubmitAndPresent(const RawFrameData * frame,const vk::SubmitInfo2& submitInfo,const vk::PresentInfoKHR& presentInfo);
  
  static vk::RenderingInfo MakeRenderingInfo(vk::Extent2D drawExtent);
  static void GenerateMipMaps(vk::CommandBuffer cmd, vk::Image image,
                              vk::Extent2D size, const vk::Filter &filter);
  
  static void TransitionImage(vk::CommandBuffer cmd, vk::Image image,
                              vk::ImageLayout from,
                              vk::ImageLayout to,
                              const std::variant<vk::ImageSubresourceRange,vk::ImageAspectFlags>&  resourceOrAspect = vk::ImageAspectFlagBits::eColor,
                              vk::PipelineStageFlags2 srcStageFlags = vk::PipelineStageFlagBits2::eAllCommands,
                              vk::PipelineStageFlags2 dstStageFlags = vk::PipelineStageFlagBits2::eAllCommands,
                              vk::AccessFlags2 srcAccessFlags = vk::AccessFlagBits2::eMemoryWrite,
                              vk::AccessFlags2 dstAccessFlags = vk::AccessFlagBits2::eMemoryWrite | vk::AccessFlagBits2::eMemoryRead
                              );
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

  Ref<WindowDrawer> GetMainWindowDrawer();
  String GetName() const override;
  
  float renderScale = 1.f;
  
  void WaitDeviceIdle();

  Ref<WindowDrawer> GetWindowDrawer(const Ref<window::Window>& window);
  
  vk::Device GetVirtualDevice() const;
  vk::PhysicalDevice GetPhysicalDevice() const;
  uint32_t GetQueueFamily() const;
  vk::Instance GetVulkanInstance() const;
  Ref<Allocator> GetAllocator() const;


  Ref<Texture2D> GetDefaultWhiteTexture() const;
  Ref<Texture2D> GetDefaultBlackTexture() const;
  Ref<Texture2D> GetDefaultGreyTexture() const;
  Ref<Texture2D> GetDefaultErrorCheckerboardTexture() const;
  
  // Default Samplers
  vk::Sampler GetDefaultSamplerLinear() const;
  vk::Sampler GetDefaultSamplerNearest() const;

  Ref<ShaderManager> GetShaderManager() const;
  
  DescriptorAllocatorGrowable * GetGlobalDescriptorAllocator();
  
  Managed<AllocatedImage> CreateImage(vk::Extent3D size,
                                      vk::Format format,
                                      vk::ImageUsageFlags usage,
                                      bool mipMapped = false) const;
  Managed<AllocatedImage> CreateImage(const void *data,
                                      vk::Extent3D size,
                                      vk::Format format,
                                      vk::ImageUsageFlags usage,
                                      bool mipMapped = false,const vk::Filter& mipMapFilter = vk::Filter::eLinear);
  
  // void CreateComputeShader(const Shader *shader, ComputeEffect &effect);

  void Init(Engine * outer) override;

  void BeforeDestroy() override;

  void ImmediateSubmit(std::function<void(vk::CommandBuffer cmd)> &&function);

  //void drawImGui(vk::CommandBuffer cmd, vk::ImageView view);

  virtual void Draw();

  Managed<GpuGeometryBuffers> CreateGeometryBuffers(const Mesh *mesh);

  template<typename T>
  Managed<GpuGeometryBuffers> CreateGeometryBuffers(const Array<T>& vertices,const Array<uint32_t>& indices);
};

template <typename T> Managed<GpuGeometryBuffers> DrawingSubsystem::CreateGeometryBuffers(const Array<T> &vertices,
    const Array<uint32_t> &indices) {
  const auto vertexBufferSize = vertices.byte_size();
  const auto indexBufferSize = indices.byte_size();

  Managed newBuffers{new GpuGeometryBuffers};

  newBuffers->vertexBuffer = GetAllocator().Reserve()->CreateBuffer(
      vertexBufferSize,
      vk::BufferUsageFlagBits::eStorageBuffer
      | vk::BufferUsageFlagBits::eTransferDst
      | vk::BufferUsageFlagBits::eShaderDeviceAddress,
      VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,
      vk::MemoryPropertyFlagBits::eDeviceLocal);

  const vk::BufferDeviceAddressInfo deviceAddressInfo{
    newBuffers->vertexBuffer->buffer};
  newBuffers->vertexBufferAddress = _device.getBufferAddress(deviceAddressInfo);

  newBuffers->indexBuffer = GetAllocator().Reserve()->CreateBuffer(
      vertexBufferSize,
      vk::BufferUsageFlagBits::eIndexBuffer
      | vk::BufferUsageFlagBits::eTransferDst,
      VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,
      vk::MemoryPropertyFlagBits::eDeviceLocal);

  const auto stagingBuffer = GetAllocator().Reserve()->
                                            CreateTransferCpuGpuBuffer(
                                                vertexBufferSize +
                                                indexBufferSize, false);

  const auto data = stagingBuffer->GetMappedData();
  memcpy(data, vertices.data(), vertexBufferSize);
  memcpy(static_cast<char *>(data) + vertexBufferSize, indices.data(),
         indexBufferSize);

  ImmediateSubmit([&](const vk::CommandBuffer cmd) {
    const vk::BufferCopy vertexCopy{0, 0, vertexBufferSize};

    cmd.copyBuffer(stagingBuffer->buffer, newBuffers->vertexBuffer->buffer, 1,
                   &vertexCopy);

    const vk::BufferCopy indicesCopy{vertexBufferSize, 0, indexBufferSize};

    cmd.copyBuffer(stagingBuffer->buffer, newBuffers->indexBuffer->buffer, 1,
                   &indicesCopy);
  });

  return newBuffers;
}

REFLECT_IMPLEMENT(DrawingSubsystem)
} // namespace rendering
} // namespace vengine
