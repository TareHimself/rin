#include <vengine/drawing/DrawingSubsystem.hpp>
#include <vengine/drawing/Allocator.hpp>
#include <vengine/drawing/Mesh.hpp>
#include <vengine/drawing/Shader.hpp>
#include <vengine/drawing/Texture.hpp>
#include "vengine/Engine.hpp"
#include "vengine/io/io.hpp"
#include <VkBootstrap.h>
#include <vengine/drawing/types.hpp>
#include "vengine/scene/Scene.hpp"
#include <vengine/drawing/scene/SceneDrawer.hpp>
#include "vengine/utils.hpp"
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

void DrawingSubsystem::InitSwapchain() {
  const auto extent = GetSwapchainExtent();

  CreateSwapchain();

  AddCleanup([=] {
    if (_swapchain != nullptr) {
      DestroySwapchain();
    }
  });
}

void DrawingSubsystem::CreateSwapchain() {
  const auto extent = GetSwapchainExtent();

  vkb::SwapchainBuilder swapchainBuilder{_gpu, _device, _surface};

  _swapchainImageFormat = vk::Format::eB8G8R8A8Unorm;

  vkb::Swapchain vkbSwapchain = swapchainBuilder
                                .set_desired_format(
                                    vk::SurfaceFormatKHR(
                                        _swapchainImageFormat,
                                        vk::ColorSpaceKHR::eSrgbNonlinear))
                                .set_desired_present_mode(
                                    VK_PRESENT_MODE_FIFO_KHR)
                                .set_desired_extent(extent.width, extent.height)
                                .add_image_usage_flags(
                                    VK_IMAGE_USAGE_TRANSFER_DST_BIT)
                                .build()
                                .value();

  _swapchain = vkbSwapchain.swapchain;
  const auto vkbSwapchainImages = vkbSwapchain.get_images().value();
  for (const auto image : vkbSwapchainImages) {
    vk::Image im = image;
    _swapchainImages.push(im);
  }

  const auto vkbSwapchainViews = vkbSwapchain.get_image_views().value();
  for (const auto image : vkbSwapchainViews) {
    vk::ImageView im = image;
    _swapchainImageViews.push(im);
  }

  vk::ImageUsageFlags drawImageUsages;
  drawImageUsages |= vk::ImageUsageFlagBits::eTransferSrc;
  drawImageUsages |= vk::ImageUsageFlagBits::eTransferDst;
  drawImageUsages |= vk::ImageUsageFlagBits::eStorage;
  drawImageUsages |= vk::ImageUsageFlagBits::eColorAttachment;

  auto drawCreateInfo = MakeImageCreateInfo(vk::Format::eR16G16B16A16Sfloat,
                                            vk::Extent3D{
                                                extent.width, extent.height, 1},
                                            drawImageUsages);
  _drawImage = _allocator->AllocateImage(drawCreateInfo,
                                         VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,
                                         vk::MemoryPropertyFlagBits::eDeviceLocal);

  const auto drawViewInfo = vk::ImageViewCreateInfo({}, _drawImage->image,
                                                    vk::ImageViewType::e2D,
                                                    _drawImage->format, {},
                                                    {vk::ImageAspectFlagBits::eColor,
                                                      0,
                                                      1, 0, 1});

  _drawImage->view = _device.createImageView(drawViewInfo);

  auto depthCreateInfo = MakeImageCreateInfo(vk::Format::eD32Sfloat,
                                             _drawImage->extent,
                                             vk::ImageUsageFlagBits::eDepthStencilAttachment);

  _depthImage = _allocator->AllocateImage(depthCreateInfo,
                                          VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,
                                          vk::MemoryPropertyFlagBits::eDeviceLocal);

  const auto depthViewInfo = vk::ImageViewCreateInfo({}, _depthImage->image,
    vk::ImageViewType::e2D,
    _depthImage->format, {},
    {vk::ImageAspectFlagBits::eDepth, 0,
     1, 0, 1});

  _depthImage->view = _device.createImageView(depthViewInfo);
}

void DrawingSubsystem::ResizeSwapchain() {

  if (_bIsResizingSwapchain) {
    return;
  }

  _bIsResizingSwapchain = true;

  GetLogger()->info("Resizing Swapchain");
  WaitDeviceIdle();

  DestroySwapchain();
  GetLogger()->info("Destroyed Old Swapchain");

  GetEngine()->NotifyWindowResize();

  CreateSwapchain();

  const auto extent = GetEngine()->GetWindowExtent();
  GetLogger()->info("Created New Swapchain");
  onResizeEvent(extent);
  // for(auto fn : _resizeCallbacks) {
  //   fn();
  // }
  _bResizeRequested = false;
  _bIsResizingSwapchain = false;
  GetLogger()->info("Swapchain Resize Completed");
}

void DrawingSubsystem::DestroySwapchain() {
  _drawImage.Clear();
  _depthImage.Clear();

  for (const auto view : _swapchainImageViews) {
    _device.destroyImageView(view);
  }

  _swapchainImageViews.clear();
  _swapchainImages.clear();

  _device.destroySwapchainKHR(_swapchain);

  _swapchain = nullptr;
}

void DrawingSubsystem::InitCommands() {
  const auto commandPoolInfo = vk::CommandPoolCreateInfo(
      vk::CommandPoolCreateFlags(
          vk::CommandPoolCreateFlagBits::eResetCommandBuffer),
      _graphicsQueueFamily);

  for (auto &_frame : _frames) {

    _frame.SetCommandPool(_device.createCommandPool(commandPoolInfo, nullptr));

    const auto commandBufferAllocateInfo = vk::CommandBufferAllocateInfo(
        *_frame.GetCmdPool(), vk::CommandBufferLevel::ePrimary, 1);

    _frame.SetCommandBuffer(
        _device.allocateCommandBuffers(commandBufferAllocateInfo)
               .
               at(0));
  }

  _immCommandPool = _device.createCommandPool(commandPoolInfo, nullptr);

  const vk::CommandBufferAllocateInfo cmdAllocInfo{
      _immCommandPool, vk::CommandBufferLevel::ePrimary, 1};

  _immediateCommandBuffer = _device.allocateCommandBuffers(cmdAllocInfo).at(0);

  AddCleanup([this] {
    _device.destroyCommandPool(_immCommandPool);

    for (auto &_frame : _frames) {
      _device.destroyCommandPool(*_frame.GetCmdPool());
    }
  });
}


void DrawingSubsystem::InitSyncStructures() {
  constexpr auto fenceCreateInfo = vk::FenceCreateInfo(
      vk::FenceCreateFlagBits::eSignaled);

  constexpr auto semaphoreCreateInfo = vk::SemaphoreCreateInfo(
      vk::SemaphoreCreateFlags());

  for (auto &_frame : _frames) {
    _frame.SetRenderFence(_device.createFence(fenceCreateInfo));
    _frame.SetSemaphores(_device.createSemaphore(semaphoreCreateInfo),
                         _device.createSemaphore(semaphoreCreateInfo));
  }

  _immediateFence = _device.createFence(fenceCreateInfo);

  AddCleanup([this] {

    _device.destroyFence(_immediateFence);

    for (const auto &_frame : _frames) {
      _device.destroyFence(_frame.GetRenderFence());
      _device.destroySemaphore(_frame.GetRenderSemaphore());
      _device.destroySemaphore(_frame.GetSwapchainSemaphore());
    }
  });

}

void DrawingSubsystem::InitPipelineLayout() {
  // auto pushConstants = {
  //     vk::PushConstantRange({vk::ShaderStageFlagBits::eVertex}, 0,
  //                           sizeof(SceneDrawPushConstants))};
  // _mainPipelineLayout = _device.createPipelineLayout(
  //     vk::PipelineLayoutCreateInfo({}, {}, pushConstants));
  // AddCleanup([this] {
  //   _device.destroyPipelineLayout(_mainPipelineLayout);
  // });
}

void DrawingSubsystem::InitPipelines() {
  // PipelineBuilder builder;
  // _mainPipeline = builder
  //                 .SetLayout(_mainPipelineLayout)
  //                 .AddVertexShader(Shader::FromSource(
  //                     _shaderManager, io::getRawShaderPath("triangle.vert")))
  //                 .AddFragmentShader(Shader::FromSource(
  //                     _shaderManager, io::getRawShaderPath("triangle.frag")))
  //                 .SetInputTopology(vk::PrimitiveTopology::eTriangleList)
  //                 .SetPolygonMode(vk::PolygonMode::eFill)
  //                 .SetCullMode(vk::CullModeFlagBits::eNone,
  //                              vk::FrontFace::eClockwise)
  //                 .SetMultisamplingModeNone()
  //                 .DisableBlending()
  //                 //.disableDepthTest()
  //                 .EnableDepthTest(true, vk::CompareOp::eLessOrEqual)
  //                 .SetColorAttachmentFormat(_drawImage.format)
  //                 .SetDepthFormat(_depthImage.format)
  //                 .Build(_device);

  // ComputeEffect gradient{"gradient"};
  // ComputeEffect gradient2{"gradient 2"};
  //
  // CreateComputeShader(
  //     Shader::FromSource(_shaderManager, io::getRawShaderPath("pretty.comp")),
  //     gradient2);
  // CreateComputeShader(
  //     Shader::FromSource(_shaderManager, io::getRawShaderPath("pretty.comp")),
  //     gradient);
  //
  // gradient.data.time = 0.0f;
  // gradient.data.data1 = {.5, .5, .5, 1};
  // gradient.data.data2 = {.5, .5, .5, 1};
  // gradient.data.data3 = {1, 1, 1, 1};
  // gradient.data.data4 = {0.263, 0.416, 0.557, 1};
  // gradient2.data.time = 0.0f;
  // gradient2.data.data1 = {.5, .5, .5, 1};
  // gradient2.data.data2 = {.5, .5, .5, 1};
  // gradient2.data.data3 = {1, 1, 1, 1};
  // gradient2.data.data4 = {0.263, 0.416, 0.557, 1};
  // backgroundEffects.Push(gradient);
  // backgroundEffects.Push(gradient2);
  //
  // AddCleanup([this] {
  //   for (const auto &effect : backgroundEffects) {
  //     _device.destroyPipeline(effect.pipeline);
  //     _device.destroyPipelineLayout(effect.layout);
  //   }
  //
  //   // _device.destroyPipeline(_mainPipeline);
  // });
}

void DrawingSubsystem::InitDescriptors() {
  // 10 sets 1 image each

  Array<DescriptorAllocatorGrowable::PoolSizeRatio> sizes = {
      {vk::DescriptorType::eStorageImage, 1}};

  _globalAllocator.Init(_device, 10, sizes);

  AddCleanup([=] {
    _globalAllocator.DestroyPools();
  });

  {
    DescriptorLayoutBuilder builder;
    builder.AddBinding(0, vk::DescriptorType::eStorageImage,
                       vk::ShaderStageFlagBits::eCompute);
    _drawImageDescriptorLayout = builder.Build(_device);

    AddCleanup([=] {
      _device.destroyDescriptorSetLayout(_drawImageDescriptorLayout);
      //descriptorAllocator.
    });
  }

  _drawImageDescriptors = _globalAllocator.Allocate(_drawImageDescriptorLayout);
  _drawImageDescriptors.Reserve()->WriteImage(0, _drawImage, {},
                                              vk::ImageLayout::eGeneral,
                                              vk::DescriptorType::eStorageImage);

  AddCleanup(onResizeEvent,onResizeEvent.Bind([=](vk::Extent2D _) {
    if (!_drawImageDescriptors)
      return;
    _drawImageDescriptors.Reserve()->WriteImage(0, _drawImage, {},
                                                vk::ImageLayout::eGeneral,
                                                vk::DescriptorType::eStorageImage);
  }));

  for (auto &frame : _frames) {
    // create a descriptor pool
    std::vector<DescriptorAllocatorGrowable::PoolSizeRatio> frame_sizes = {
        {vk::DescriptorType::eStorageImage, 3},
        {vk::DescriptorType::eStorageBuffer, 3},
        {vk::DescriptorType::eUniformBuffer, 3},
        {vk::DescriptorType::eCombinedImageSampler, 4},
    };

    frame.SetDrawer(this);
    frame.GetDescriptorAllocator()->Init(_device, 1000, frame_sizes);

    AddCleanup([&] {
      frame.GetDescriptorAllocator()->DestroyPools();
      frame.cleaner.Run();
    });
  }
}

// void Drawer::initImGui() {
//   constexpr auto poolSize = 1000;
//   Array<vk::DescriptorPoolSize> poolSizes = {
//       {vk::DescriptorType::eSampler, poolSize},
//       {vk::DescriptorType::eCombinedImageSampler, poolSize},
//       {vk::DescriptorType::eSampledImage, poolSize},
//       {vk::DescriptorType::eStorageImage, poolSize},
//       {vk::DescriptorType::eUniformTexelBuffer, poolSize},
//       {vk::DescriptorType::eStorageTexelBuffer, poolSize},
//       {vk::DescriptorType::eUniformBuffer, poolSize},
//       {vk::DescriptorType::eStorageBuffer, poolSize},
//       {vk::DescriptorType::eUniformBufferDynamic, poolSize},
//       {vk::DescriptorType::eStorageBufferDynamic, poolSize},
//       {vk::DescriptorType::eInputAttachment, poolSize}
//   };
//
//   const vk::DescriptorPoolCreateInfo poolInfo{
//       vk::DescriptorPoolCreateFlagBits::eFreeDescriptorSet, poolSize,
//       poolSizes};
//
//   const vk::DescriptorPool imGuiPool = _device.createDescriptorPool(poolInfo);
//
//   ImGui::CreateContext();
//
//   ImGui_ImplSDL3_InitForVulkan(getEngine()->getWindow());
//
//   ImGui_ImplVulkan_InitInfo initInfo{instance, _gpu, _device,
//                                      _graphicsQueueFamily, _graphicsQueue,
//                                      nullptr, imGuiPool};
//   initInfo.MinImageCount = 3;
//   initInfo.ImageCount = 3;
//   initInfo.UseDynamicRendering = true;
//   initInfo.ColorAttachmentFormat = static_cast<VkFormat>(_swapchainImageFormat);
//
//   initInfo.MSAASamples = static_cast<VkSampleCountFlagBits>(
//     vk::SampleCountFlagBits::e1);
//
//   ImGui_ImplVulkan_Init(&initInfo, nullptr);
//
//   // immediateSubmit([&] (vk::CommandBuffer cmd){
//   //   ImGui_ImplVulkan_CreateFontsTexture();
//   // });
//
//   addCleanup([=] {
//     ImGui_ImplVulkan_Shutdown();
//     ImGui_ImplSDL3_Shutdown();
//     _device.destroyDescriptorPool(imGuiPool);
//   });
// }

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

  _whiteTexture = Texture::FromData(this, whiteData, vk::Extent3D{1, 1, 1},
                                    vk::Format::eR8G8B8A8Unorm,
                                    vk::Filter::eLinear);

  constexpr uint32_t grey = 0xAAAAAAFF;
  auto greyData = Array<unsigned char>(sizeof(uint32_t));
  memcpy(greyData.data(), &grey, greyData.size());
  _greyTexture = Texture::FromData(this, greyData, vk::Extent3D{1, 1, 1},
                                   vk::Format::eR8G8B8A8Unorm,
                                   vk::Filter::eLinear);

  constexpr uint32_t black = 0x000000FF;
  auto blackData = Array<unsigned char>(sizeof(uint32_t));
  memcpy(blackData.data(), &black, blackData.size());
  _blackTexture = Texture::FromData(this, blackData, vk::Extent3D{1, 1, 1},
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

  _errorCheckerboardTexture = Texture::FromData(this, checkerBoardData,
                                                vk::Extent3D{16, 16, 1},
                                                vk::Format::eR8G8B8A8Unorm,
                                                vk::Filter::eLinear);

  AddCleanup([=] {
    _errorCheckerboardTexture.Clear();
    _blackTexture.Clear();
    _greyTexture.Clear();
    _whiteTexture.Clear();
    GetDevice().destroySampler(_defaultSamplerLinear);
    GetDevice().destroySampler(_defaultSamplerNearest);
  });
}

void DrawingSubsystem::InitDefaultMaterials() {
  // MaterialInstance::MaterialResources materialResources;
  // materialResources.color = _errorCheckerboardTexture;
  // materialResources.colorSampler = _defaultSamplerLinear;
  // materialResources.metallic = _whiteTexture;
  // materialResources.metallicSampler = _defaultSamplerLinear;
  //
  // materialResources.dataBuffer = nullptr;
  // materialResources.dataBufferOffset = 0;

}

void DrawingSubsystem::TransitionImage(const vk::CommandBuffer cmd, const vk::Image image,
                             const vk::ImageLayout currentLayout,
                             const vk::ImageLayout newLayout) {
  vk::ImageMemoryBarrier2 imageBarrier;
  imageBarrier
      .setSrcStageMask(vk::PipelineStageFlagBits2::eAllCommands)
      .setSrcAccessMask(vk::AccessFlagBits2::eMemoryWrite)
      .setDstStageMask(vk::PipelineStageFlagBits2::eAllCommands)
      .setDstAccessMask(
          vk::AccessFlagBits2::eMemoryWrite | vk::AccessFlagBits2::eMemoryRead)
      .setOldLayout(currentLayout)
      .setNewLayout(newLayout);

  const vk::ImageAspectFlags aspectMask = (newLayout ==
                                           vk::ImageLayout::eDepthAttachmentOptimal)
                                            ? vk::ImageAspectFlagBits::eDepth
                                            : vk::ImageAspectFlagBits::eColor;
  imageBarrier.setSubresourceRange(ImageSubResourceRange(aspectMask));

  imageBarrier.setImage(image);

  vk::DependencyInfo depInfo;
  depInfo.setImageMemoryBarriers({imageBarrier});

  cmd.pipelineBarrier2(&depInfo);
}

vk::RenderingInfo DrawingSubsystem::MakeRenderingInfo(vk::Extent2D drawExtent) {
  return vk::RenderingInfo({}, {{0, 0}, drawExtent}, 1, {});
}

void DrawingSubsystem::GenerateMipMaps(const vk::CommandBuffer cmd, const vk::Image image,
                             vk::Extent2D size,const vk::Filter& filter) {
  const int mipLevels = static_cast<int>(std::floor(
                            std::log2(std::max(size.width, size.height)))) + 1;

  for (int mip = 0; mip < mipLevels; mip++) {

    VkExtent2D halfSize = size;
    halfSize.width /= 2;
    halfSize.height /= 2;

    vk::ImageMemoryBarrier2 imageBarrier{};

    
    
    imageBarrier.setSrcStageMask(vk::PipelineStageFlagBits2::eAllCommands);
    imageBarrier.setSrcAccessMask(vk::AccessFlagBits2::eMemoryWrite);
    imageBarrier.setDstStageMask(vk::PipelineStageFlagBits2::eAllCommands);
    imageBarrier.setDstAccessMask(
        vk::AccessFlagBits2::eMemoryWrite | vk::AccessFlagBits2::eMemoryRead);

    imageBarrier.setOldLayout(vk::ImageLayout::eTransferDstOptimal);
    imageBarrier.setNewLayout(vk::ImageLayout::eTransferSrcOptimal);
    
    constexpr vk::ImageAspectFlags aspectMask = vk::ImageAspectFlagBits::eColor;
    imageBarrier.subresourceRange = ImageSubResourceRange(aspectMask);
    imageBarrier.subresourceRange.setLevelCount(1);
    imageBarrier.subresourceRange.setBaseMipLevel(mip);
    imageBarrier.setImage(image);

    vk::DependencyInfo depInfo{};
    depInfo.setImageMemoryBarriers(imageBarrier);
    cmd.pipelineBarrier2(depInfo);

    if (mip < mipLevels - 1) {
      // vk::ImageBlit2 blitRegion{};
      // blitRegion.srcOffsets[1].x = size.width;
      // blitRegion.srcOffsets[1].y = size.height;
      // blitRegion.srcOffsets[1].z = 1;
      //
      // blitRegion.dstOffsets[1].x = halfSize.width;
      // blitRegion.dstOffsets[1].y = halfSize.height;
      // blitRegion.dstOffsets[1].z = 1;
      //
      // blitRegion.srcSubresource.setAspectMask(vk::ImageAspectFlagBits::eColor);
      // blitRegion.srcSubresource.setBaseArrayLayer(0);
      // blitRegion.srcSubresource.setLayerCount(1);
      // blitRegion.srcSubresource.setMipLevel(mip);
      //
      // blitRegion.dstSubresource.setAspectMask(vk::ImageAspectFlagBits::eColor);
      // blitRegion.dstSubresource.setBaseArrayLayer(0);
      // blitRegion.dstSubresource.setLayerCount(1);
      // blitRegion.dstSubresource.setMipLevel(mip + 1);
      //
      // vk::BlitImageInfo2 blitInfo{};
      // blitInfo.setDstImage(image);
      // blitInfo.setDstImageLayout(vk::ImageLayout::eTransferDstOptimal);
      // blitInfo.setSrcImage(image);
      // blitInfo.setSrcImageLayout(vk::ImageLayout::eTransferSrcOptimal);
      // blitInfo.setFilter(vk::Filter::eLinear);
      // blitInfo.setRegions(blitRegion);
      //
      // cmd.blitImage2(blitInfo);

      auto blitRegion = vk::ImageBlit2();
      blitRegion.setSrcOffsets(
      {vk::Offset3D{},
       vk::Offset3D{static_cast<int>(size.width),
                    static_cast<int>(size.height), 1}});

      blitRegion.setDstOffsets(
      {vk::Offset3D{},
       vk::Offset3D{static_cast<int>(halfSize.width),
                    static_cast<int>(halfSize.height), 1}});

      blitRegion.setSrcSubresource({vk::ImageAspectFlagBits::eColor, static_cast<uint32_t>(mip), 0, 1});

      blitRegion.setDstSubresource({vk::ImageAspectFlagBits::eColor, static_cast<uint32_t>(mip + 1), 0, 1});

      const auto blitInfo = vk::BlitImageInfo2(
          image, vk::ImageLayout::eTransferSrcOptimal,
          image, vk::ImageLayout::eTransferDstOptimal,
          {blitRegion}, filter);

      cmd.blitImage2(blitInfo);
      
      size = halfSize;
    }
  }

  // transition all mip levels into the final read_only layout
  TransitionImage(cmd, image, vk::ImageLayout::eTransferSrcOptimal,
                   vk::ImageLayout::eShaderReadOnlyOptimal);
}

RawFrameData *DrawingSubsystem::GetCurrentFrame() {
  return &_frames[_frameCount % FRAME_OVERLAP];
}

void DrawingSubsystem::WaitDeviceIdle() {
  _deviceMutex.lock();
  _device.waitIdle();
  _deviceMutex.unlock();
}

void DrawingSubsystem::DrawBackground(RawFrameData *frame) const {
  const auto cmd = frame->GetCmd();

  //float flash = abs(sin(_frameCount / 120.f));

  const auto clearValue = vk::ClearColorValue({0.0f, 0.0f, 0.0f, 0.0f});
  //vk::ClearColorValue({0.0f, 0.0f, flash, 0.0f});

  auto clearRange = ImageSubResourceRange(vk::ImageAspectFlagBits::eColor);

  cmd->clearColorImage(_drawImage->image,
                       vk::ImageLayout::eGeneral, clearValue,
                       {clearRange});

  // auto computeEffect = backgroundEffects.at(currentBackgroundEffect);
  // computeEffect.data.time = GetEngine()->GetEngineTimeSeconds();
  // cmd->bindPipeline(vk::PipelineBindPoint::eCompute, computeEffect.pipeline);
  //
  // cmd->bindDescriptorSets(vk::PipelineBindPoint::eCompute,
  //                         computeEffect.layout, 0, {_drawImageDescriptors},
  //                         {});
  //
  // cmd->pushConstants(computeEffect.layout, vk::ShaderStageFlagBits::eCompute, 0,
  //                    computeEffect.size, &computeEffect.data);
  // const auto extent = GetEngine()->GetWindowExtent();
  //
  // cmd->dispatch(std::ceil(extent.width / 16.0), std::ceil(extent.height / 16.0),
  //               1);
}

void DrawingSubsystem::DrawScenes(RawFrameData *frame) {
  const vk::Extent2D drawExtent = GetDrawImageExtentScaled();

  const auto colorAttachment = MakeRenderingAttachment(
      _drawImage->view, vk::ImageLayout::eGeneral);

  vk::ClearValue depthClear;
  depthClear.setDepthStencil({1.f});

  const auto depthAttachment = MakeRenderingAttachment(
      _depthImage->view, vk::ImageLayout::eDepthAttachmentOptimal, depthClear);

  auto renderingInfo = MakeRenderingInfo(drawExtent);
  renderingInfo.setColorAttachments(colorAttachment);
  renderingInfo.setPDepthAttachment(&depthAttachment);

  const auto cmd = frame->GetCmd();

  cmd->beginRendering(renderingInfo);

  //Actual Rendering
  for (const auto &scene : GetEngine()->GetScenes()) {
    if (auto sceneRef = scene.Reserve(); sceneRef->IsInitialized()) {
      if (auto sceneDrawer = sceneRef->GetDrawer().Reserve(); sceneDrawer->
        IsInitialized()) {
        sceneDrawer->Draw(frame);
      }
    }
  }

  cmd->endRendering();
}

void DrawingSubsystem::DrawUI(RawFrameData *frame) {
  const vk::Extent2D drawExtent = GetDrawImageExtentScaled();

  const auto colorAttachment =
      MakeRenderingAttachment(_drawImage->view, vk::ImageLayout::eGeneral);

  auto renderingInfo = MakeRenderingInfo(drawExtent);
  renderingInfo.setColorAttachments(colorAttachment);

  const auto cmd = frame->GetCmd();

  cmd->beginRendering(renderingInfo);

  // Actual Rendering
  const auto widgetManager = GetEngine()->GetWidgetSubsystem().Reserve();

  widgetManager->Draw(frame);

  cmd->endRendering();
}

void DrawingSubsystem::CopyImageToImage(const vk::CommandBuffer cmd, const vk::Image src,
                              const vk::Image dst, const vk::Extent2D srcSize,
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
      {blitRegion}, vk::Filter::eLinear);

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

vk::ImageViewCreateInfo DrawingSubsystem::MakeImageViewCreateInfo(vk::Format format,
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

vk::Extent2D DrawingSubsystem::GetSwapchainExtent() const {
  return GetEngine()->GetWindowExtent();
}

vk::Extent2D DrawingSubsystem::GetSwapchainExtentScaled() const {
  const auto extent = GetSwapchainExtent();

  return {static_cast<uint32_t>(renderScale * extent.width),
          static_cast<uint32_t>(renderScale * extent.height)};
}

vk::Extent2D DrawingSubsystem::GetDrawImageExtent() const {
  if (!_drawImage) {
    return {};
  }
  return {static_cast<uint32_t>(_drawImage->extent.width),
          static_cast<uint32_t>(_drawImage->extent.height)};
}

vk::Extent2D DrawingSubsystem::GetDrawImageExtentScaled() const {
  return {static_cast<uint32_t>(_drawImage->extent.width * renderScale),
          static_cast<uint32_t>(_drawImage->extent.height * renderScale)};
}

vk::Format DrawingSubsystem::GetDrawImageFormat() const {
  return _drawImage->format;
}

vk::Format DrawingSubsystem::GetDepthImageFormat() const {
  return _depthImage->format;
}

bool DrawingSubsystem::ResizePending() const {
  return _bResizeRequested;
}

void DrawingSubsystem::RequestResize() {
  _bResizeRequested = true;
}

bool DrawingSubsystem::IsResizingSwapchain() const {
  return _bIsResizingSwapchain;
}

void DrawingSubsystem::CreateComputeShader(const Shader *shader,
                                 ComputeEffect &effect) {

  vk::PushConstantRange pushConstant{{vk::ShaderStageFlagBits::eCompute}, 0,
                                     effect.size};

  // Compute Pipeline Creation
  const vk::PipelineLayoutCreateInfo computeLayoutCreateInfo{
      {}, {_drawImageDescriptorLayout}, {pushConstant}};

  effect.layout = _device.
      createPipelineLayout(computeLayoutCreateInfo);
  const vk::PipelineShaderStageCreateInfo stageInfo{
      {}, vk::ShaderStageFlagBits::eCompute, *shader, "main"};

  vk::ComputePipelineCreateInfo computePipelineCreateInfo{
      {}, stageInfo, effect.layout};

  effect.pipeline = _device.createComputePipelines(
      nullptr, {computePipelineCreateInfo}).value.at(0);
}

void DrawingSubsystem::Init(Engine *outer) {
  EngineSubsystem::Init(outer);
  AddCleanup(onResizeEvent,onResizeEvent.Bind([=](const vk::Extent2D size) {
    GetOuter()->onWindowSizeChanged(size);
  }));

  vkb::InstanceBuilder builder;

  auto [numExtensions,extensions] = window::getExtensions();
  auto instanceResult =
      builder.set_app_name(GetEngine()->GetAppName().c_str())
             .require_api_version(1, 3, 0)
             .request_validation_layers(true)
      .enable_extensions(numExtensions,extensions)
#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
            .use_default_debug_messenger()
#endif
      
  .build();

  auto vkbInstance = instanceResult.value();
  _instance = vkbInstance.instance;

#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
  debugMessenger = vkbInstance.debug_messenger;
#endif

  auto window = GetEngine()->GetWindow().Reserve();
  
  _surface = window->CreateSurface(_instance);

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
              .set_surface(_surface)
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
    _instance.destroySurfaceKHR(_surface);

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

  InitSwapchain();

  InitCommands();

  //initDefaultRenderPass();

  //initFrameBuffers();

  InitSyncStructures();

  InitDescriptors();

  _shaderManager = newManagedObject<ShaderManager>();
  _shaderManager->Init(this);

  InitPipelineLayout();

  InitPipelines();

  //initImGui();

  InitDefaultTextures();

  InitDefaultMaterials();
}

vk::Device DrawingSubsystem::GetDevice() const {
  return _device;
}

vk::PhysicalDevice DrawingSubsystem::GetPhysicalDevice() const {
  return _gpu;
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

  _graphicsQueue.submit2({vk::SubmitInfo2{{}, {}, {cmdInfo}}}, _immediateFence);

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

String DrawingSubsystem::GetName() const {
  return "drawing";
}

Managed<AllocatedImage> DrawingSubsystem::CreateImage(
    const vk::Extent3D size, const vk::Format format,
    const vk::ImageUsageFlags usage, const bool mipMapped) const {

  auto imgInfo = MakeImageCreateInfo(format, size, usage);
  if (mipMapped) {
    imgInfo.setMipLevels(CalcMipLevels({size.width,size.height}));
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
    const bool mipMapped,const vk::Filter& mipMapFilter) {

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
      GenerateMipMaps(cmd, newImage->image,{newImage->extent.width,newImage->extent.height},mipMapFilter);
    } else {
      TransitionImage(cmd, newImage->image,
                      vk::ImageLayout::eTransferDstOptimal,
                      vk::ImageLayout::eShaderReadOnlyOptimal);
    }

  });

  return newImage;
}


Managed<GpuMeshBuffers> DrawingSubsystem::CreateMeshBuffers(const Mesh *mesh) {
  const auto vertices = mesh->GetVertices();
  const auto indices = mesh->GetIndices();
  const auto vertexBufferSize = vertices.byte_size();
  const auto indexBufferSize = indices.byte_size();

  Managed<GpuMeshBuffers> newBuffers{new GpuMeshBuffers};

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

  ImmediateSubmit([=](const vk::CommandBuffer cmd) {
    const vk::BufferCopy vertexCopy{0, 0, vertexBufferSize};

    cmd.copyBuffer(stagingBuffer->buffer, newBuffers->vertexBuffer->buffer, 1,
                   &vertexCopy);

    const vk::BufferCopy indicesCopy{vertexBufferSize, 0, indexBufferSize};

    cmd.copyBuffer(stagingBuffer->buffer, newBuffers->indexBuffer->buffer, 1,
                   &indicesCopy);
  });

  return newBuffers;
}

Ref<Allocator> DrawingSubsystem::GetAllocator() const {
  return _allocator;
}

Ref<Texture> DrawingSubsystem::GetDefaultWhiteTexture() const {
  return _whiteTexture;
}

Ref<Texture> DrawingSubsystem::GetDefaultBlackTexture() const {
  return _blackTexture;
}

Ref<Texture> DrawingSubsystem::GetDefaultGreyTexture() const {
  return _greyTexture;
}

Ref<Texture> DrawingSubsystem::GetDefaultErrorCheckerboardTexture() const {
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
  if (_bResizeRequested && !_bIsResizingSwapchain) {
    ResizeSwapchain();
    return;
  }

  const auto frame = GetCurrentFrame();
  // Wait for gpu to finish past work
  vk::resultCheck(
      _device.waitForFences({frame->GetRenderFence()}, true, 1000000000),
      "Wait For Fences Failed");

  if (_bResizeRequested && !_bIsResizingSwapchain) {
    ResizeSwapchain();
    return;
  }

  frame->cleaner.Run();
  frame->GetDescriptorAllocator()->ClearPools();

  _device.resetFences({frame->GetRenderFence()});

  // Request image index from swapchain
  uint32_t swapchainImageIndex;

  try {
    const auto _ = _device.acquireNextImageKHR(_swapchain, 1000000000,
                                               frame->GetSwapchainSemaphore(),
                                               nullptr, &swapchainImageIndex);
  } catch (vk::OutOfDateKHRError &_) {
    _bResizeRequested = true;
    return;
  }

  if (_bResizeRequested && !_bIsResizingSwapchain) {
    ResizeSwapchain();
    return;
  }

  const auto cmd = frame->GetCmd();

  // Clear command buffer and prepare to render
  cmd->reset();

  constexpr auto commandBeginInfo = vk::CommandBufferBeginInfo(
      vk::CommandBufferUsageFlagBits::eOneTimeSubmit);

  const auto swapchainExtent = GetSwapchainExtent();

  const vk::Extent2D drawExtent = GetDrawImageExtentScaled();

  cmd->begin(commandBeginInfo);

  // Transition image to general layout
  TransitionImage(*cmd, _drawImage->image,
                  vk::ImageLayout::eUndefined, vk::ImageLayout::eGeneral);

  DrawBackground(frame);

  TransitionImage(*cmd, _drawImage->image,
                  vk::ImageLayout::eGeneral,
                  vk::ImageLayout::eColorAttachmentOptimal);
  TransitionImage(*cmd, _depthImage->image,
                  vk::ImageLayout::eUndefined,
                  vk::ImageLayout::eDepthAttachmentOptimal);

  DrawScenes(frame);

  DrawUI(frame);

  // Transition images to correct transfer layouts
  TransitionImage(*cmd, _drawImage->image,
                  vk::ImageLayout::eColorAttachmentOptimal,
                  vk::ImageLayout::eTransferSrcOptimal);
  TransitionImage(*cmd, _swapchainImages[swapchainImageIndex],
                  vk::ImageLayout::eUndefined,
                  vk::ImageLayout::eTransferDstOptimal);

  CopyImageToImage(*cmd, _drawImage->image,
                   _swapchainImages[swapchainImageIndex],
                   drawExtent, swapchainExtent);

  TransitionImage(*cmd, _swapchainImages[swapchainImageIndex],
                  vk::ImageLayout::eTransferDstOptimal,
                  vk::ImageLayout::eColorAttachmentOptimal);

  //drawImGui(*cmd, _swapchainImageViews[swapchainImageIndex]);

  TransitionImage(*cmd, _swapchainImages[swapchainImageIndex],
                  vk::ImageLayout::eColorAttachmentOptimal,
                  vk::ImageLayout::ePresentSrcKHR
      );

  // Cant add commands anymore
  cmd->end();

  const auto cmdInfo = vk::CommandBufferSubmitInfo(*cmd, 0);

  const auto waitingInfo = vk::SemaphoreSubmitInfo(
      frame->GetSwapchainSemaphore(), 1,
      vk::PipelineStageFlagBits2::eColorAttachmentOutput);
  const auto signalInfo = vk::SemaphoreSubmitInfo(
      frame->GetRenderSemaphore(), 1, vk::PipelineStageFlagBits2::eAllGraphics);

  const auto submitInfo = vk::SubmitInfo2({}, {waitingInfo}, {cmdInfo},
                                          {signalInfo});

  _queueMutex.lock();
  _graphicsQueue.submit2({submitInfo}, frame->GetRenderFence());
  _queueMutex.unlock();
  const auto renderSemaphore = frame->GetRenderSemaphore();
  const auto presentInfo = vk::PresentInfoKHR({renderSemaphore},
                                              {_swapchain},
                                              swapchainImageIndex);

  try {
    std::lock_guard guard(_queueMutex);
    const auto _ = _graphicsQueue.presentKHR(presentInfo);
  } catch (vk::OutOfDateKHRError &_) {
    _bResizeRequested = true;
    return;
  }

  _frameCount++;
}
}
