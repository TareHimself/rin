#include <vengine/drawing/DrawingSubsystem.hpp>
#include <vengine/drawing/Allocator.hpp>
#include <vengine/drawing/Mesh.hpp>
#include <vengine/drawing/Shader.hpp>
#include <vengine/drawing/Texture2D.hpp>
#include "vengine/Engine.hpp"
#include "vengine/io/io.hpp"
#include <VkBootstrap.h>
#include <vengine/drawing/types.hpp>
#include "vengine/scene/Scene.hpp"
#include <vengine/drawing/scene/SceneDrawer.hpp>
#include "vengine/utils.hpp"
#include "vengine/drawing/WindowDrawer.hpp"
#include "vengine/widget/WidgetSubsystem.hpp"
#include "vengine/window/Window.hpp"


// we want to immediately abort when there is an error. In normal engines this
// would give an error message to the user, or perform a dump of state.
using namespace std;
#define VULKAN_HPP_DISABLE_ENHANCED_MODE
#define VK_CHECK(x)                                                            \
  do {                                                                         \
    VkResult err = x;                                                          \
    if (err) {                                                                 \
      std::cout << "Detected Vulkan error: " << err << std::endl;              \
      abort();                                                                 \
    }                                                                          \
  } while (0)

namespace vengine::drawing {

void DrawingSubsystem::InitCommands() {
  const auto commandPoolInfo = vk::CommandPoolCreateInfo(
      vk::CommandPoolCreateFlags(
          vk::CommandPoolCreateFlagBits::eResetCommandBuffer),
      _graphicsQueueFamily);

  _immediateCommandPool = _device.createCommandPool(commandPoolInfo, nullptr);
  const vk::CommandBufferAllocateInfo cmdAllocInfo{
      _immediateCommandPool, vk::CommandBufferLevel::ePrimary, 1};

  _immediateCommandBuffer = _device.allocateCommandBuffers(cmdAllocInfo).at(0);

  AddCleanup([this] {
    _device.destroyCommandPool(_immediateCommandPool);
  });
}


void DrawingSubsystem::InitSyncStructures() {
  constexpr auto fenceCreateInfo = vk::FenceCreateInfo(
      vk::FenceCreateFlagBits::eSignaled);

  _immediateFence = _device.createFence(fenceCreateInfo);

  AddCleanup([this] {

    _device.destroyFence(_immediateFence);
  });

}

// void DrawingSubsystem::InitPipelineLayout() {
//   // auto pushConstants = {
//   //     vk::PushConstantRange({vk::ShaderStageFlagBits::eVertex}, 0,
//   //                           sizeof(SceneDrawPushConstants))};
//   // _mainPipelineLayout = _device.createPipelineLayout(
//   //     vk::PipelineLayoutCreateInfo({}, {}, pushConstants));
//   // AddCleanup([this] {
//   //   _device.destroyPipelineLayout(_mainPipelineLayout);
//   // });
// }

// void DrawingSubsystem::InitPipelines() {
//   // PipelineBuilder builder;
//   // _mainPipeline = builder
//   //                 .SetLayout(_mainPipelineLayout)
//   //                 .AddVertexShader(Shader::FromSource(
//   //                     _shaderManager, io::getRawShaderPath("triangle.vert")))
//   //                 .AddFragmentShader(Shader::FromSource(
//   //                     _shaderManager, io::getRawShaderPath("triangle.frag")))
//   //                 .SetInputTopology(vk::PrimitiveTopology::eTriangleList)
//   //                 .SetPolygonMode(vk::PolygonMode::eFill)
//   //                 .SetCullMode(vk::CullModeFlagBits::eNone,
//   //                              vk::FrontFace::eClockwise)
//   //                 .SetMultisamplingModeNone()
//   //                 .DisableBlending()
//   //                 //.disableDepthTest()
//   //                 .EnableDepthTest(true, vk::CompareOp::eLessOrEqual)
//   //                 .SetColorAttachmentFormat(_drawImage.format)
//   //                 .SetDepthFormat(_depthImage.format)
//   //                 .Build(_device);
//
//   // ComputeEffect gradient{"gradient"};
//   // ComputeEffect gradient2{"gradient 2"};
//   //
//   // CreateComputeShader(
//   //     Shader::FromSource(_shaderManager, io::getRawShaderPath("pretty.comp")),
//   //     gradient2);
//   // CreateComputeShader(
//   //     Shader::FromSource(_shaderManager, io::getRawShaderPath("pretty.comp")),
//   //     gradient);
//   //
//   // gradient.data.time = 0.0f;
//   // gradient.data.data1 = {.5, .5, .5, 1};
//   // gradient.data.data2 = {.5, .5, .5, 1};
//   // gradient.data.data3 = {1, 1, 1, 1};
//   // gradient.data.data4 = {0.263, 0.416, 0.557, 1};
//   // gradient2.data.time = 0.0f;
//   // gradient2.data.data1 = {.5, .5, .5, 1};
//   // gradient2.data.data2 = {.5, .5, .5, 1};
//   // gradient2.data.data3 = {1, 1, 1, 1};
//   // gradient2.data.data4 = {0.263, 0.416, 0.557, 1};
//   // backgroundEffects.Push(gradient);
//   // backgroundEffects.Push(gradient2);
//   //
//   // AddCleanup([this] {
//   //   for (const auto &effect : backgroundEffects) {
//   //     _device.destroyPipeline(effect.pipeline);
//   //     _device.destroyPipelineLayout(effect.layout);
//   //   }
//   //
//   //   // _device.destroyPipeline(_mainPipeline);
//   // });
// }

void DrawingSubsystem::InitDescriptors() {
  // 10 sets 1 image each

  Array<DescriptorAllocatorGrowable::PoolSizeRatio> sizes = {
      {vk::DescriptorType::eStorageImage, 1},
      {vk::DescriptorType::eUniformBuffer, 1},
      {vk::DescriptorType::eCombinedImageSampler, 1}};

  _globalAllocator.Init(_device, 10, sizes);

  AddCleanup([this] {
    _globalAllocator.DestroyPools();
  });
}

void DrawingSubsystem::InitDefaultTextures() {

  vk::SamplerCreateInfo samplerInfo;
  samplerInfo.setMagFilter(vk::Filter::eNearest);
  samplerInfo.setMinFilter(vk::Filter::eNearest);

  _defaultSamplerNearest = _device.createSampler(samplerInfo);

  samplerInfo.setMagFilter(vk::Filter::eLinear);
  samplerInfo.setMinFilter(vk::Filter::eLinear);

  _defaultSamplerLinear = _device.createSampler(samplerInfo);

  //3 default textures, white, grey, black. 1 pixel each
  constexpr uint32_t white = 0xFFFFFFFF;
  auto whiteData = Array<unsigned char>(sizeof(uint32_t));
  memcpy(whiteData.data(), &white, whiteData.size());

  _whiteTexture = Texture2D::FromData(whiteData, vk::Extent3D{1, 1, 1},
                                      vk::Format::eR8G8B8A8Unorm,
                                      vk::Filter::eLinear);

  constexpr uint32_t grey = 0xAAAAAAFF;
  auto greyData = Array<unsigned char>(sizeof(uint32_t));
  memcpy(greyData.data(), &grey, greyData.size());
  _greyTexture = Texture2D::FromData(greyData, vk::Extent3D{1, 1, 1},
                                     vk::Format::eR8G8B8A8Unorm,
                                     vk::Filter::eLinear);

  constexpr uint32_t black = 0x000000FF;
  auto blackData = Array<unsigned char>(sizeof(uint32_t));
  memcpy(blackData.data(), &black, blackData.size());
  _blackTexture = Texture2D::FromData(blackData, vk::Extent3D{1, 1, 1},
                                      vk::Format::eR8G8B8A8Unorm,
                                      vk::Filter::eLinear);

  //checkerboard image
  constexpr uint32_t magenta = 0xFF00FFFF;
  Array<uint32_t> pixels{}; //for 16x16 checkerboard texture
  pixels.resize(16 * 16);
  // std::array<uint32_t, 16 * 16> pixels; 
  for (int x = 0; x < 16; x++) {
    for (int y = 0; y < 16; y++) {
      pixels[y * 16 + x] = ((x % 2) ^ (y % 2)) ? magenta : black;
    }
  }

  Array<unsigned char> checkerBoardData;
  checkerBoardData.resize(pixels.size() * sizeof(uint32_t));
  memcpy(checkerBoardData.data(), pixels.data(), checkerBoardData.size());

  _errorCheckerboardTexture = Texture2D::FromData(
      checkerBoardData, vk::Extent3D{16, 16, 1},
      vk::Format::eR8G8B8A8Unorm,
      vk::Filter::eLinear);

  AddCleanup([this] {
    _errorCheckerboardTexture.Clear();
    _blackTexture.Clear();
    _greyTexture.Clear();
    _whiteTexture.Clear();
    GetVirtualDevice().destroySampler(_defaultSamplerLinear);
    GetVirtualDevice().destroySampler(_defaultSamplerNearest);
  });
}

vk::RenderingInfo DrawingSubsystem::MakeRenderingInfo(vk::Extent2D drawExtent) {
  return vk::RenderingInfo({}, {{0, 0}, drawExtent}, 1, {});
}

void DrawingSubsystem::GenerateMipMaps(const vk::CommandBuffer cmd,
                                       const vk::Image image,
                                       vk::Extent2D size,
                                       const vk::Filter &filter) {
  const int mipLevels = static_cast<int>(std::floor(
                            std::log2(std::max(size.width, size.height)))) + 1;

  for (int mip = 0; mip < mipLevels; mip++) {

    VkExtent2D halfSize = size;
    halfSize.width /= 2;
    halfSize.height /= 2;

    vk::ImageMemoryBarrier2 imageBarrier{};

    TransitionImage(cmd, image, vk::ImageLayout::eTransferDstOptimal,
                    vk::ImageLayout::eTransferSrcOptimal,
                    ImageSubResourceRange(vk::ImageAspectFlagBits::eColor).setLevelCount(1).setBaseMipLevel(mip));

    if (mip < mipLevels - 1) {

      auto blitRegion = vk::ImageBlit2();
      blitRegion.setSrcOffsets(
      {vk::Offset3D{},
       vk::Offset3D{static_cast<int>(size.width),
                    static_cast<int>(size.height), 1}});

      blitRegion.setDstOffsets(
      {vk::Offset3D{},
       vk::Offset3D{static_cast<int>(halfSize.width),
                    static_cast<int>(halfSize.height), 1}});

      blitRegion.setSrcSubresource({vk::ImageAspectFlagBits::eColor,
                                    static_cast<uint32_t>(mip), 0, 1});

      blitRegion.setDstSubresource({vk::ImageAspectFlagBits::eColor,
                                    static_cast<uint32_t>(mip + 1), 0, 1});

      const auto blitInfo = vk::BlitImageInfo2(
          image, vk::ImageLayout::eTransferSrcOptimal,
          image, vk::ImageLayout::eTransferDstOptimal,
          blitRegion, filter);

      cmd.blitImage2(blitInfo);

      size = halfSize;
    }
  }

  TransitionImage(cmd, image, vk::ImageLayout::eTransferSrcOptimal,
                    vk::ImageLayout::eShaderReadOnlyOptimal);
}

void DrawingSubsystem::TransitionImage(vk::CommandBuffer cmd, vk::Image image,
                                       vk::ImageLayout from, vk::ImageLayout to,
                                       const std::variant<
                                         vk::ImageSubresourceRange,
                                         vk::ImageAspectFlags> &
                                       resourceOrAspect,
                                       vk::PipelineStageFlags2 srcStageFlags,
                                       vk::PipelineStageFlags2 dstStageFlags,
                                       vk::AccessFlags2 srcAccessFlags,
                                       vk::AccessFlags2 dstAccessFlags
                                       ) {
  vk::ImageMemoryBarrier2 imageBarrier;
  imageBarrier
      .setSrcStageMask(srcStageFlags)
      .setSrcAccessMask(srcAccessFlags)
      .setDstStageMask(dstStageFlags)
      .setDstAccessMask(dstAccessFlags)
      .setOldLayout(from)
      .setNewLayout(to);

  imageBarrier.setSubresourceRange(
      std::holds_alternative<vk::ImageSubresourceRange>(resourceOrAspect)
        ? std::get<vk::ImageSubresourceRange>(resourceOrAspect)
        : ImageSubResourceRange(
            std::get<vk::ImageAspectFlags>(resourceOrAspect)));

  imageBarrier.setImage(image);

  vk::DependencyInfo depInfo;
  depInfo.setImageMemoryBarriers(imageBarrier);

  cmd.pipelineBarrier2(&depInfo);
}

void DrawingSubsystem::WaitDeviceIdle() {
  _queueMutex.lock();
  _device.waitIdle();
  _queueMutex.unlock();
}

Ref<WindowDrawer> DrawingSubsystem::GetWindowDrawer(
    const Ref<window::Window> &window) {
  if (_windowDrawers.contains(window.Reserve()->GetId())) {
    return _windowDrawers[window.Reserve()->GetId()];
  }

  return {};
}

void DrawingSubsystem::CopyImageToImage(const vk::CommandBuffer cmd,
                                        const vk::Image src,
                                        const vk::Image dst,
                                        const vk::Extent2D srcSize,
                                        const vk::Extent2D dstSize) {
  auto blitRegion = vk::ImageBlit2();
  blitRegion.setSrcOffsets(
  {vk::Offset3D{},
   vk::Offset3D{static_cast<int>(srcSize.width),
                static_cast<int>(srcSize.height), 1}});

  blitRegion.setDstOffsets(
  {vk::Offset3D{},
   vk::Offset3D{static_cast<int>(dstSize.width),
                static_cast<int>(dstSize.height), 1}});

  blitRegion.setSrcSubresource({vk::ImageAspectFlagBits::eColor, 0, 0, 1});

  blitRegion.setDstSubresource({vk::ImageAspectFlagBits::eColor, 0, 0, 1});

  const auto blitInfo = vk::BlitImageInfo2(
      src, vk::ImageLayout::eTransferSrcOptimal,
      dst, vk::ImageLayout::eTransferDstOptimal,
      blitRegion, vk::Filter::eLinear);

  cmd.blitImage2(blitInfo);
}

vk::ImageCreateInfo DrawingSubsystem::MakeImageCreateInfo(vk::Format format,
  vk::Extent3D size,
  vk::ImageUsageFlags usage) {
  return {{}, vk::ImageType::e2D,
          format, size, 1,
          1, vk::SampleCountFlagBits::e1,
          vk::ImageTiling::eOptimal,
          usage};
}

vk::ImageViewCreateInfo DrawingSubsystem::MakeImageViewCreateInfo(
    vk::Format format,
    vk::Image image, vk::ImageAspectFlags aspect) {
  return {{}, image,
          vk::ImageViewType::e2D,
          format, {},
          {aspect, 0,
           1, 0, 1}};
}

vk::ImageSubresourceRange DrawingSubsystem::ImageSubResourceRange(
    vk::ImageAspectFlags aspectMask) {
  return {aspectMask, 0, vk::RemainingMipLevels, 0,
          vk::RemainingArrayLayers};
}

// void DrawingSubsystem::CreateComputeShader(const Shader *shader,
//                                  ComputeEffect &effect) {
//
//   vk::PushConstantRange pushConstant{{vk::ShaderStageFlagBits::eCompute}, 0,
//                                      effect.size};
//
//   // Compute Pipeline Creation
//   const vk::PipelineLayoutCreateInfo computeLayoutCreateInfo{
//       {}, _drawImageDescriptorLayout, pushConstant};
//
//   effect.layout = _device.
//       createPipelineLayout(computeLayoutCreateInfo);
//   const vk::PipelineShaderStageCreateInfo stageInfo{
//       {}, vk::ShaderStageFlagBits::eCompute, *shader, "main"};
//
//   vk::ComputePipelineCreateInfo computePipelineCreateInfo{
//       {}, stageInfo, effect.layout};
//
//   effect.pipeline = _device.createComputePipelines(
//       nullptr, {computePipelineCreateInfo}).value.at(0);
// }

void DrawingSubsystem::Init(Engine *outer) {
  EngineSubsystem::Init(outer);

  vkb::InstanceBuilder builder;

  auto [numExtensions,extensions] = window::getExtensions();
  auto instanceResult =
      builder.set_app_name(GetEngine()->GetAppName().c_str())
             .require_api_version(1, 3, 0)
             //.request_validation_layers(true)
             .enable_extensions(numExtensions, extensions)
#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
            .use_default_debug_messenger()
#endif

      .build();

  auto vkbInstance = instanceResult.value();
  _instance = vkbInstance.instance;

#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
  debugMessenger = vkbInstance.debug_messenger;
#endif

  auto mainWindow = GetEngine()->GetMainWindow();

  auto mainWindowDrawer = CreateWindowDrawer(mainWindow).Reserve();

  vk::PhysicalDeviceVulkan13Features features;
  features.dynamicRendering = true;
  features.synchronization2 = true;

  vk::PhysicalDeviceVulkan12Features features12;
  features12.setBufferDeviceAddress(true)
            .setDescriptorIndexing(true)
            .setDescriptorBindingSampledImageUpdateAfterBind(true)
            .setDescriptorBindingStorageImageUpdateAfterBind(true)
            .setDescriptorBindingUniformBufferUpdateAfterBind(true);

  vkb::PhysicalDeviceSelector selector{vkbInstance};
  vkb::PhysicalDevice physicalDevice =
      selector.set_minimum_version(1, 3)
              .set_required_features_13(features)
              .set_required_features_12(features12)
              .set_surface(mainWindowDrawer->GetSurface())
              .select()
              .value();

  vkb::DeviceBuilder deviceBuilder{physicalDevice};

  vkb::Device vkbDevice = deviceBuilder.build().value();

  _device = vkbDevice.device;

  _gpu = physicalDevice.physical_device;

  _graphicsQueue = vkbDevice.get_queue(vkb::QueueType::graphics).value();

  _graphicsQueueFamily = vkbDevice.get_queue_index(vkb::QueueType::graphics).
                                   value();

  AddCleanup([this] {
    _shaderManager.Clear();

    _device.destroy();

#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
instance.destroyDebugUtilsMessengerEXT(debugMessenger);
#endif

    _instance.destroy();
  });

  auto allocatorCreateInfo = VmaAllocatorCreateInfo{};
  allocatorCreateInfo.flags = VMA_ALLOCATOR_CREATE_BUFFER_DEVICE_ADDRESS_BIT;
  allocatorCreateInfo.device = _device;
  allocatorCreateInfo.physicalDevice = _gpu;
  allocatorCreateInfo.instance = _instance;

  _allocator = newManagedObject<Allocator>();
  _allocator->Init(this);

  AddCleanup([&] {
    _allocator.Clear();
  });

  InitCommands();

  InitSyncStructures();

  InitDescriptors();

  _shaderManager = newManagedObject<ShaderManager>();
  _shaderManager->Init(this);

  InitDefaultTextures();

  mainWindowDrawer->CreateResources();

  AddCleanup(window::getManager()->onWindowCreated,
             window::getManager()->onWindowCreated.Bind(
                 [this](const Ref<window::Window> &window) {
                   auto winDrawer = CreateWindowDrawer(window);
                   winDrawer.Reserve()->CreateResources();
                 }));

  AddCleanup(window::getManager()->onWindowDestroyed,
             window::getManager()->onWindowDestroyed.Bind(
                 [this](const Ref<window::Window> &window) {
                   // if(auto drawer = GetWindowDrawer(window)) {
                   //   _windowDrawers.erase(_windowDrawers.find(window.Reserve()->GetId()));
                   // }
                 }));

  AddCleanup([this] {
    _windowDrawers.clear();
  });
}

vk::Device DrawingSubsystem::GetVirtualDevice() const {
  return _device;
}

vk::PhysicalDevice DrawingSubsystem::GetPhysicalDevice() const {
  return _gpu;
}

uint32_t DrawingSubsystem::GetQueueFamily() const {
  return _graphicsQueueFamily;
}

vk::Instance DrawingSubsystem::GetVulkanInstance() const {
  return _instance;
}


void DrawingSubsystem::BeforeDestroy() {
  // Wait for the device to idle
  WaitDeviceIdle();

  Object::BeforeDestroy();
}

void DrawingSubsystem::ImmediateSubmit(
    std::function<void(vk::CommandBuffer cmd)> &&function) {
  std::lock_guard withLock(_queueMutex);

  _device.resetFences({_immediateFence});
  _immediateCommandBuffer.reset();

  const auto cmd = _immediateCommandBuffer;

  cmd.begin({vk::CommandBufferUsageFlagBits::eOneTimeSubmit});

  function(cmd);

  cmd.end();

  vk::CommandBufferSubmitInfo cmdInfo{cmd, 0};

  _graphicsQueue.submit2(vk::SubmitInfo2{{}, {}, cmdInfo}, _immediateFence);

  vk::resultCheck(_device.waitForFences({_immediateFence}, true,UINT64_MAX),
                  "Failed to wait for fences for immediate submit");
}

vk::RenderingAttachmentInfo DrawingSubsystem::MakeRenderingAttachment(
    const vk::ImageView view,
    const vk::ImageLayout layout, const std::optional<vk::ClearValue> &clear) {
  vk::RenderingAttachmentInfo attachment{view, layout};

  attachment.loadOp = clear.has_value()
                        ? vk::AttachmentLoadOp::eClear
                        : vk::AttachmentLoadOp::eLoad;
  attachment.storeOp = vk::AttachmentStoreOp::eStore;
  if (clear.has_value()) {
    attachment.clearValue = clear.value();
  }

  return attachment;
}

uint32_t DrawingSubsystem::CalcMipLevels(const vk::Extent2D &extent) {
  return static_cast<uint32_t>(std::floor(
             std::log2(std::max(extent.width, extent.height)))) + 1;
}

Ref<WindowDrawer> DrawingSubsystem::GetMainWindowDrawer() {
  return GetWindowDrawer(GetEngine()->GetMainWindow());
}

Ref<WindowDrawer> DrawingSubsystem::CreateWindowDrawer(
    const Ref<window::Window> &window) {
  auto drawer = newManagedObject<WindowDrawer>();
  drawer->Init(window, this);
  _windowDrawers.emplace(window.Reserve()->GetId(), drawer);
  return drawer;
}

void DrawingSubsystem::SubmitAndPresent(const RawFrameData *frame,
                                        const vk::SubmitInfo2 &submitInfo,
                                        const vk::PresentInfoKHR &presentInfo) {
  std::lock_guard guard(_queueMutex);
  _graphicsQueue.submit2(submitInfo, frame->GetRenderFence());
  const auto _ = _graphicsQueue.presentKHR(presentInfo);
}


String DrawingSubsystem::GetName() const {
  return "drawing";
}

Managed<AllocatedImage> DrawingSubsystem::CreateImage(
    const vk::Extent3D size, const vk::Format format,
    const vk::ImageUsageFlags usage, const bool mipMapped) const {

  auto imgInfo = MakeImageCreateInfo(format, size, usage);
  if (mipMapped) {
    imgInfo.setMipLevels(CalcMipLevels({size.width, size.height}));
  }

  // allocate and create the image
  auto newImage = GetAllocator().Reserve()->AllocateImage(
      imgInfo, VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,
      vk::MemoryPropertyFlagBits::eDeviceLocal);

  vk::ImageAspectFlags aspectFlags = vk::ImageAspectFlagBits::eColor;
  if (format == vk::Format::eD32Sfloat) {
    aspectFlags = vk::ImageAspectFlagBits::eDepth;
  }

  // Build an image view for the image
  vk::ImageViewCreateInfo viewInfo = MakeImageViewCreateInfo(
      format, newImage->image, aspectFlags);
  viewInfo.subresourceRange.setLevelCount(imgInfo.mipLevels);

  newImage->view = _device.createImageView(viewInfo);

  return newImage;
}

Managed<AllocatedImage> DrawingSubsystem::CreateImage(
    const void *data, const vk::Extent3D size,
    const vk::Format format, const vk::ImageUsageFlags usage,
    const bool mipMapped, const vk::Filter &mipMapFilter) {

  auto channels = 0;
  switch (format) {
  case vk::Format::eR8G8B8Unorm:
    channels = 3;
    break;
  case vk::Format::eR8G8B8A8Unorm:
    channels = 4;
    break;
  default:
    channels = 4;
  }

  const auto dataSize = size.depth * size.width * size.height * channels;

  const auto uploadBuffer = GetAllocator().Reserve()->
                                           CreateTransferCpuGpuBuffer(
                                               dataSize, false);

  const auto mapped = uploadBuffer->GetMappedData();

  utils::vassert(mapped != nullptr && data != nullptr, "WE DONE FUCKED UP");
  memcpy(mapped, data, dataSize);

  auto newImage = CreateImage(size, format,
                              usage |
                              vk::ImageUsageFlagBits::eTransferDst
                              | vk::ImageUsageFlagBits::eTransferSrc,
                              mipMapped);

  ImmediateSubmit([&](const vk::CommandBuffer cmd) {
    TransitionImage(cmd, newImage->image, vk::ImageLayout::eUndefined,
                    vk::ImageLayout::eTransferDstOptimal);

    vk::BufferImageCopy copyRegion{0, 0, 0};
    copyRegion.setImageSubresource({vk::ImageAspectFlagBits::eColor, 0, 0, 1});
    copyRegion.setImageExtent(size);

    cmd.copyBufferToImage(uploadBuffer->buffer, newImage->image,
                          vk::ImageLayout::eTransferDstOptimal, 1, &copyRegion);

    if (mipMapped) {
      GenerateMipMaps(cmd, newImage->image,
                      {newImage->extent.width, newImage->extent.height},
                      mipMapFilter);
    } else {
      TransitionImage(cmd, newImage->image,
                      vk::ImageLayout::eTransferDstOptimal,
                      vk::ImageLayout::eShaderReadOnlyOptimal);
    }

  });

  return newImage;
}


Managed<GpuGeometryBuffers> DrawingSubsystem::CreateGeometryBuffers(const Mesh *mesh) {
  const auto vertices = mesh->GetVertices();
  const auto indices = mesh->GetIndices();

  return CreateGeometryBuffers(vertices,indices);
}

Ref<Allocator> DrawingSubsystem::GetAllocator() const {
  return _allocator;
}

Ref<Texture2D> DrawingSubsystem::GetDefaultWhiteTexture() const {
  return _whiteTexture;
}

Ref<Texture2D> DrawingSubsystem::GetDefaultBlackTexture() const {
  return _blackTexture;
}

Ref<Texture2D> DrawingSubsystem::GetDefaultGreyTexture() const {
  return _greyTexture;
}

Ref<Texture2D> DrawingSubsystem::GetDefaultErrorCheckerboardTexture() const {
  return _errorCheckerboardTexture;
}

vk::Sampler DrawingSubsystem::GetDefaultSamplerLinear() const {
  return _defaultSamplerLinear;
}

vk::Sampler DrawingSubsystem::GetDefaultSamplerNearest() const {
  return _defaultSamplerNearest;
}

Ref<ShaderManager> DrawingSubsystem::GetShaderManager() const {
  return _shaderManager;
}

DescriptorAllocatorGrowable *DrawingSubsystem::GetGlobalDescriptorAllocator() {
  return &_globalAllocator;
}

void DrawingSubsystem::Draw() {

  for (auto &window : _windowDrawers | views::values) {
    if (window->ShouldDraw()) {
      window->Draw();
    }

  }
}
}
