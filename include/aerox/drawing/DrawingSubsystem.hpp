#pragma once
#include "Shader.hpp"
#include "ShaderManager.hpp"
#include "descriptors.hpp"
#include "types.hpp"
#include "aerox/EngineSubsystem.hpp"
#include "aerox/Object.hpp"
#include "aerox/containers/Array.hpp"
#include <vulkan/vulkan.hpp>
#include <future>
#include <queue>
#include "gen/drawing/DrawingSubsystem.gen.hpp"

namespace aerox::window {
class Window;
}

namespace aerox {
class Engine;
}

namespace aerox::scene {
class Scene;
class CameraComponent;
}

namespace aerox::drawing {

class Viewport;
class WindowDrawer;
class Texture;
class Allocator;
class MaterialInstance;
class Mesh;


constexpr unsigned int FRAME_OVERLAP = 2;

struct GraphicsQueueOp {
  std::function<void(const vk::Queue &)> func;
  std::promise<std::optional<std::exception_ptr>> *pending;
};

/**
 * \brief Base class for the engine renderer. uses vulkan
 */

META_TYPE()

class DrawingSubsystem : public EngineSubsystem {

  vk::Instance _instance = nullptr;
  std::mutex _deviceMutex;
  std::mutex _queueMutex;
#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
  vk::DebugUtilsMessengerEXT _debugMessenger = nullptr;
#endif

  vk::PhysicalDevice _gpu = nullptr;
  vk::Device _device = nullptr;
  vk::SurfaceKHR _surface = nullptr;
  vk::Queue _graphicsQueue = nullptr;
  uint32_t _graphicsQueueFamily = -1;

  // Default Images
  std::shared_ptr<Texture> _whiteTexture;
  std::shared_ptr<Texture> _blackTexture;
  std::shared_ptr<Texture> _greyTexture;
  std::shared_ptr<Texture> _errorCheckerboardTexture;

  // Default Samplers
  vk::Sampler _defaultSamplerLinear;
  vk::Sampler _defaultSamplerNearest;

  DescriptorAllocatorGrowable _globalAllocator{};

  vk::Fence _immediateFence;
  vk::CommandBuffer _immediateCommandBuffer;
  vk::CommandPool _immediateCommandPool;

  std::shared_ptr<Allocator> _allocator;

  std::shared_ptr<ShaderManager> _shaderManager;
  Array<std::function<void()>> _resizeCallbacks;

  std::unordered_map<uint64_t, std::shared_ptr<WindowDrawer>> _windowDrawers;
  std::queue<GraphicsQueueOp> _submitQueue{};
  std::thread _submitThread;
  std::condition_variable _submitCond;

protected:
  void InitCommands();

  void InitSyncStructures();

  void InitDescriptors();

  void InitDefaultTextures();

  std::weak_ptr<WindowDrawer> CreateWindowDrawer(const std::weak_ptr<window::Window> &window);

public:
  static std::shared_ptr<meta::Metadata> Meta;
  std::shared_ptr<meta::Metadata> GetMeta() const override;

  vk::Format GetSwapchainFormat();
  
  void SubmitThreadSafe(const vk::SubmitInfo2 &info, const vk::Fence &fence);

  void RunQueueOperation(const std::function<void(const vk::Queue &)> &func);

  void SubmitAndPresent(const RawFrameData *frame,
                        const vk::SubmitInfo2 &submitInfo,
                        const vk::PresentInfoKHR &presentInfo);

  static vk::RenderingInfo MakeRenderingInfo(vk::Extent2D drawExtent);
  static void GenerateMipMaps(vk::CommandBuffer cmd, vk::Image image,
                              vk::Extent2D size, const vk::Filter &filter);

  static void TransitionImage(vk::CommandBuffer cmd, vk::Image image,
                              vk::ImageLayout from,
                              vk::ImageLayout to,
                              const std::variant<
                                vk::ImageSubresourceRange, vk::ImageAspectFlags>
                              &resourceOrAspect =
                                  vk::ImageAspectFlagBits::eColor,
                              vk::PipelineStageFlags2 srcStageFlags =
                                  vk::PipelineStageFlagBits2::eAllCommands,
                              vk::PipelineStageFlags2 dstStageFlags =
                                  vk::PipelineStageFlagBits2::eAllCommands,
                              vk::AccessFlags2 srcAccessFlags =
                                  vk::AccessFlagBits2::eMemoryWrite,
                              vk::AccessFlags2 dstAccessFlags =
                                  vk::AccessFlagBits2::eMemoryWrite |
                                  vk::AccessFlagBits2::eMemoryRead
      );
  static vk::ImageSubresourceRange ImageSubResourceRange(
      vk::ImageAspectFlags aspectMask);
  static void CopyImageToImage(vk::CommandBuffer cmd, vk::Image src,
                               vk::Image dst, vk::Extent2D srcSize,
                               vk::Extent2D dstSize);

  static vk::ImageCreateInfo MakeImageCreateInfo(
      vk::Format format, vk::Extent3D size, vk::ImageUsageFlags usage);

  static vk::ImageViewCreateInfo MakeImageViewCreateInfo(
      vk::Format format, vk::Image image, vk::ImageAspectFlags aspect);

  static vk::RenderingAttachmentInfo MakeRenderingAttachment(
      vk::ImageView view,
      vk::ImageLayout layout = vk::ImageLayout::eAttachmentOptimal,
      const std::optional<vk::ClearValue> &clear = std::nullopt);

  static uint32_t CalcMipLevels(const vk::Extent2D &extent);

  std::weak_ptr<WindowDrawer> GetMainWindowDrawer();
  String GetName() const override;

  float renderScale = 1.f;

  void WaitDeviceIdle();

  std::weak_ptr<WindowDrawer> GetWindowDrawer(const std::weak_ptr<window::Window> &window);

  vk::Device GetVirtualDevice() const;
  vk::PhysicalDevice GetPhysicalDevice() const;
  uint32_t GetQueueFamily() const;
  vk::Instance GetVulkanInstance() const;
  std::weak_ptr<Allocator> GetAllocator() const;


  std::weak_ptr<Texture> GetDefaultWhiteTexture() const;
  std::weak_ptr<Texture> GetDefaultBlackTexture() const;
  std::weak_ptr<Texture> GetDefaultGreyTexture() const;
  std::weak_ptr<Texture> GetDefaultErrorCheckerboardTexture() const;

  // Default Samplers
  vk::Sampler GetDefaultSamplerLinear() const;
  vk::Sampler GetDefaultSamplerNearest() const;

  std::weak_ptr<ShaderManager> GetShaderManager() const;

  DescriptorAllocatorGrowable *GetGlobalDescriptorAllocator();

  std::shared_ptr<AllocatedImage> CreateImage(vk::Extent3D size,
                                      vk::Format format,
                                      vk::ImageUsageFlags usage,
                                      bool mipMapped = false,const std::string& name = "Image") const;
  std::shared_ptr<AllocatedImage> CreateImage(const void *data,
                                      vk::Extent3D size,
                                      vk::Format format,
                                      vk::ImageUsageFlags usage,
                                      bool mipMapped = false,
                                      const vk::Filter &mipMapFilter =
                                          vk::Filter::eLinear,const std::string& name = "Image");

  // void CreateComputeShader(const Shader *shader, ComputeEffect &effect);

  void OnInit(Engine *outer) override;

  void OnDestroy() override;

  void ImmediateSubmit(std::function<void(vk::CommandBuffer cmd)> &&function);

  //void drawImGui(vk::CommandBuffer cmd, vk::ImageView view);

  virtual void Draw();

  std::shared_ptr<GpuGeometryBuffers> CreateGeometryBuffers(const Mesh *mesh);

  template <typename T>
  std::shared_ptr<GpuGeometryBuffers> CreateGeometryBuffers(
      const Array<T> &vertices, const Array<uint32_t> &indices);
};

template <typename T> std::shared_ptr<GpuGeometryBuffers>
DrawingSubsystem::CreateGeometryBuffers(const Array<T> &vertices,
                                        const Array<uint32_t> &indices) {
  const auto vertexBufferSize = vertices.byte_size();
  const auto indexBufferSize = indices.byte_size();

  auto newBuffers = std::make_shared<GpuGeometryBuffers>();

  newBuffers->vertexBuffer = GetAllocator().lock()->CreateBuffer(
      vertexBufferSize,
      vk::BufferUsageFlagBits::eStorageBuffer
      | vk::BufferUsageFlagBits::eTransferDst
      | vk::BufferUsageFlagBits::eShaderDeviceAddress,
      VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,
      vk::MemoryPropertyFlagBits::eDeviceLocal);

  const vk::BufferDeviceAddressInfo deviceAddressInfo{
      newBuffers->vertexBuffer->buffer};
  newBuffers->vertexBufferAddress = _device.getBufferAddress(deviceAddressInfo);

  newBuffers->indexBuffer = GetAllocator().lock()->CreateBuffer(
      vertexBufferSize,
      vk::BufferUsageFlagBits::eIndexBuffer
      | vk::BufferUsageFlagBits::eTransferDst,
      VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,
      vk::MemoryPropertyFlagBits::eDeviceLocal);

  const auto stagingBuffer = GetAllocator().lock()->
                                            CreateTransferCpuGpuBuffer(
                                                vertexBufferSize +
                                                indexBufferSize, false);

  stagingBuffer->Write(vertices.data(), vertexBufferSize);
  stagingBuffer->Write(indices.data(), indexBufferSize, vertexBufferSize);

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
}
