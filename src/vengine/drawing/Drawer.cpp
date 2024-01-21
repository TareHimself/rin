
#include "Drawer.hpp"

#include "Allocator.hpp"
#include "MaterialBuilder.hpp"
#include "MaterialInstance.hpp"
#include "Mesh.hpp"
#include "PipelineBuilder.hpp"
#include "Shader.hpp"
#include "Texture.hpp"
#include "vengine/Engine.hpp"
#include "vengine/io/io.hpp"
#include <VkBootstrap.h>
#include <SDL3/SDL.h>
#include <SDL3/SDL_vulkan.h>
#include "types.hpp"
#include "vengine/scene/Scene.hpp"
#include <vk_mem_alloc.h>
#include "scene/SceneDrawer.hpp"
#include "vengine/utils.hpp"


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

namespace vengine {
namespace drawing {

void Drawer::InitSwapchain() {
  const auto extent = GetSwapchainExtent();

  CreateSwapchain();

  AddCleanup([=] {
    if (_swapchain != nullptr) {
      DestroySwapchain();
    }
  });
}

void Drawer::CreateSwapchain() {
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
    _swapchainImages.Push(im);
  }

  const auto vkbSwapchainViews = vkbSwapchain.get_image_views().value();
  for (const auto image : vkbSwapchainViews) {
    vk::ImageView im = image;
    _swapchainImageViews.Push(im);
  }

  vk::ImageUsageFlags drawImageUsages;
  drawImageUsages |= vk::ImageUsageFlagBits::eTransferSrc;
  drawImageUsages |= vk::ImageUsageFlagBits::eTransferDst;
  drawImageUsages |= vk::ImageUsageFlagBits::eStorage;
  drawImageUsages |= vk::ImageUsageFlagBits::eColorAttachment;

  auto drawCreateInfo = MakeImageCreateInfo(vk::Format::eR16G16B16A16Sfloat,vk::Extent3D{extent.width, extent.height, 1},drawImageUsages);
  _Allocator->AllocateImage(_drawImage,drawCreateInfo,VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE, vk::MemoryPropertyFlagBits::eDeviceLocal);

  const auto drawViewInfo = vk::ImageViewCreateInfo({}, _drawImage.image,
                                                vk::ImageViewType::e2D,
                                                _drawImage.format, {},
                                                {vk::ImageAspectFlagBits::eColor, 0,
                                                 1, 0, 1});
  
  _drawImage.view = _device.createImageView(drawViewInfo);

  
  auto depthCreateInfo = MakeImageCreateInfo(vk::Format::eD32Sfloat,_drawImage.extent,vk::ImageUsageFlagBits::eDepthStencilAttachment);
  
  _Allocator->AllocateImage(_depthImage,depthCreateInfo,VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE, vk::MemoryPropertyFlagBits::eDeviceLocal);
  
  const auto depthViewInfo = vk::ImageViewCreateInfo({}, _depthImage.image,
                                                vk::ImageViewType::e2D,
                                                _depthImage.format, {},
                                                {vk::ImageAspectFlagBits::eDepth, 0,
                                                 1, 0, 1});
  
  _depthImage.view = _device.createImageView(depthViewInfo);
}

void Drawer::ResizeSwapchain() {
  if(!_resizePending) {
    _resizePending = true;
    return;
  }
  log::drawing->info("Resizing Swapchain");
  _device.waitIdle();

  DestroySwapchain();
  log::drawing->info("Destroyed Old Swapchain");

  GetEngine()->NotifyWindowResize();

  CreateSwapchain();

  const auto extent = GetEngine()->GetWindowExtent();
  log::drawing->info("Created New Swapchain");
  onResizeEvent.Emit(extent);
  // for(auto fn : _resizeCallbacks) {
  //   fn();
  // }
  _resizePending = false;
  log::drawing->info("Swapchain Resize Completed");
}

void Drawer::DestroySwapchain() {

  _device.destroyImageView(_drawImage.view);
  GetAllocator()->DestroyImage(_drawImage);

  _device.destroyImageView(_depthImage.view);
  GetAllocator()->DestroyImage(_depthImage);
  
  for (const auto view : _swapchainImageViews) {
    _device.destroyImageView(view);
  }

  _swapchainImageViews.clear();
  _swapchainImages.clear();

  _device.destroySwapchainKHR(_swapchain);

  _swapchain = nullptr;
}

void Drawer::InitCommands() {
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


void Drawer::InitSyncStructures() {
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

void Drawer::InitPipelineLayout() {
  // auto pushConstants = {
  //     vk::PushConstantRange({vk::ShaderStageFlagBits::eVertex}, 0,
  //                           sizeof(SceneDrawPushConstants))};
  // _mainPipelineLayout = _device.createPipelineLayout(
  //     vk::PipelineLayoutCreateInfo({}, {}, pushConstants));
  // AddCleanup([this] {
  //   _device.destroyPipelineLayout(_mainPipelineLayout);
  // });
}

void Drawer::InitPipelines() {
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

  ComputeEffect gradient{"gradient"};
  ComputeEffect gradient2{"gradient 2"};

  CreateComputeShader(
      Shader::FromSource(_shaderManager, io::getRawShaderPath("pretty.comp")),
      gradient2);
  CreateComputeShader(
      Shader::FromSource(_shaderManager, io::getRawShaderPath("pretty.comp")),
      gradient);

  gradient.data.time = 0.0f;
  gradient.data.data1 = {.5, .5, .5, 1};
  gradient.data.data2 = {.5, .5, .5, 1};
  gradient.data.data3 = {1, 1, 1, 1};
  gradient.data.data4 = {0.263, 0.416, 0.557, 1};
  gradient2.data.time = 0.0f;
  gradient2.data.data1 = {.5, .5, .5, 1};
  gradient2.data.data2 = {.5, .5, .5, 1};
  gradient2.data.data3 = {1, 1, 1, 1};
  gradient2.data.data4 = {0.263, 0.416, 0.557, 1};
  backgroundEffects.Push(gradient);
  backgroundEffects.Push(gradient2);

  AddCleanup([this] {
    for (const auto &effect : backgroundEffects) {
      _device.destroyPipeline(effect.pipeline);
      _device.destroyPipelineLayout(effect.layout);
    }

    // _device.destroyPipeline(_mainPipeline);
  });
}

void Drawer::InitDescriptors() {
  // 10 sets 1 image each

  {
    const auto device = GetDevice();
    DescriptorLayoutBuilder builder;
    builder.AddBinding(0, vk::DescriptorType::eUniformBuffer);
    _sceneDescriptorSetLayout = builder.Build(device,
                                              vk::ShaderStageFlagBits::eVertex
                                              | vk::ShaderStageFlagBits::eFragment);

    AddCleanup([=] {
      device.destroyDescriptorSetLayout(_sceneDescriptorSetLayout);
    });
  }

  Array<DescriptorAllocatorGrowable::PoolSizeRatio> sizes = {
      {vk::DescriptorType::eStorageImage, 1}};

  _globalAllocator.Init(_device, 10, sizes);

  AddCleanup([=] {
    _globalAllocator.DestroyPools();
  });

  {
    DescriptorLayoutBuilder builder;
    builder.AddBinding(0, vk::DescriptorType::eStorageImage);
    _drawImageDescriptorLayout = builder.Build(_device,
                                               vk::ShaderStageFlagBits::eCompute);

    AddCleanup([=] {
      _device.destroyDescriptorSetLayout(_drawImageDescriptorLayout);
      //descriptorAllocator.
    });
  }

  _drawImageDescriptors = _globalAllocator.Allocate(
      _drawImageDescriptorLayout);

  DescriptorWriter writer;
  writer.WriteImage(0, _drawImage.view, {}, vk::ImageLayout::eGeneral,
                    vk::DescriptorType::eStorageImage);

  writer.UpdateSet(_device, _drawImageDescriptors);

  onResizeEvent.On([=] (vk::Extent2D _){
    DescriptorWriter writer;
    writer.WriteImage(0, _drawImage.view, {}, vk::ImageLayout::eGeneral,
                    vk::DescriptorType::eStorageImage);

    writer.UpdateSet(_device, _drawImageDescriptors);
  });

  for (auto &_frame : _frames) {
    // create a descriptor pool
    std::vector<DescriptorAllocatorGrowable::PoolSizeRatio> frame_sizes = {
        {vk::DescriptorType::eStorageImage, 3},
        {vk::DescriptorType::eStorageBuffer, 3},
        {vk::DescriptorType::eUniformBuffer, 3},
        {vk::DescriptorType::eCombinedImageSampler, 4},
    };

    _frame.GetDescriptorAllocator()->Init(_device, 1000, frame_sizes);

    AddCleanup([&] {
      _frame.GetDescriptorAllocator()->DestroyPools();
      _frame.cleaner.Run();
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

void Drawer::InitDefaultTextures() {

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
  memcpy(whiteData.data(),&white,whiteData.size());
  
  _whiteTexture = Texture::FromData(this,whiteData,vk::Extent3D{1, 1, 1},
                            vk::Format::eR8G8B8A8Unorm,
                            vk::Filter::eLinear);

  
  constexpr uint32_t grey = 0xAAAAAAFF;
  auto greyData = Array<unsigned char>(sizeof(uint32_t));
  memcpy(greyData.data(),&grey,greyData.size());
  _greyTexture = Texture::FromData(this,greyData,vk::Extent3D{1, 1, 1},
                            vk::Format::eR8G8B8A8Unorm,
                            vk::Filter::eLinear);
  
  constexpr uint32_t black = 0x000000FF;
  auto blackData = Array<unsigned char>(sizeof(uint32_t));
  memcpy(blackData.data(),&black,blackData.size());
  _blackTexture = Texture::FromData(this,blackData,vk::Extent3D{1, 1, 1},
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
  memcpy(checkerBoardData.data(),pixels.data(),checkerBoardData.size());

  _errorCheckerboardTexture = Texture::FromData(this, checkerBoardData,vk::Extent3D{16, 16, 1},
                            vk::Format::eR8G8B8A8Unorm,
                            vk::Filter::eLinear);

  AddCleanup([=] {
    _errorCheckerboardTexture->Destroy();
    _blackTexture->Destroy();
    _greyTexture->Destroy();
    _whiteTexture->Destroy();
    GetDevice().destroySampler(_defaultSamplerLinear);
    GetDevice().destroySampler(_defaultSamplerNearest);
  });
}

void Drawer::InitDefaultMaterials() {
  // MaterialInstance::MaterialResources materialResources;
  // materialResources.color = _errorCheckerboardTexture;
  // materialResources.colorSampler = _defaultSamplerLinear;
  // materialResources.metallic = _whiteTexture;
  // materialResources.metallicSampler = _defaultSamplerLinear;
  //
  // materialResources.dataBuffer = nullptr;
  // materialResources.dataBufferOffset = 0;

  MaterialBuilder builder;
  _defaultCheckeredMaterial = builder
  .SetPass(EMaterialPass::Opaque)
  .AddShader(Shader::FromSource(GetShaderManager(), io::getRawShaderPath("mesh.frag")),vk::ShaderStageFlagBits::eFragment)
  .AddShader(Shader::FromSource(GetShaderManager(), io::getRawShaderPath("mesh.vert")),vk::ShaderStageFlagBits::eVertex)
  .Create(this);

  
  const auto resources = _defaultCheckeredMaterial->GetResources();
  for(auto resource : resources) {
    if(resource.second.type == EMaterialResourceType::Image) {
      _defaultCheckeredMaterial->SetTexture(resource.first,GetDefaultBlackTexture());
    }
  }

  _defaultCheckeredMaterial->SetTexture("ColorT",GetDefaultErrorCheckerboardTexture());
   //= MaterialInstance::create(this,EMaterialPass::Opaque,_globalAllocator,materialResources);
  AddCleanup([=] {
    _defaultCheckeredMaterial->Destroy();
    _defaultCheckeredMaterial = nullptr;
  });
}

void Drawer::TransitionImage(const vk::CommandBuffer cmd, const vk::Image image,
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

vk::RenderingInfo Drawer::MakeRenderingInfo(vk::Extent2D drawExtent) {
  return vk::RenderingInfo({}, {{0, 0}, drawExtent}, 1, {});
}

FrameData *Drawer::GetCurrentFrame() {
  return &_frames[_frameCount % FRAME_OVERLAP];
}

void Drawer::DrawBackground(FrameData *frame) const {
  const auto cmd = frame->GetCmd();

  //float flash = abs(sin(_frameCount / 120.f));

  const auto clearValue = vk::ClearColorValue({0.0f, 0.0f, 0.0f, 0.0f});//vk::ClearColorValue({0.0f, 0.0f, flash, 0.0f});

  auto clearRange = ImageSubResourceRange(vk::ImageAspectFlagBits::eColor);

  cmd->clearColorImage(_drawImage.image,
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

void Drawer::DrawScenes(FrameData *frame) {
  const vk::Extent2D drawExtent = GetDrawImageExtent();

  const auto colorAttachment = MakeRenderingAttachment(
      _drawImage.view, vk::ImageLayout::eGeneral);

  vk::ClearValue depthClear;
  depthClear.setDepthStencil({1.f});

  const auto depthAttachment = MakeRenderingAttachment(
      _depthImage.view, vk::ImageLayout::eDepthAttachmentOptimal, depthClear);

  auto renderingInfo = MakeRenderingInfo(drawExtent);
  renderingInfo.setColorAttachments(colorAttachment);
  renderingInfo.setPDepthAttachment(&depthAttachment);

  const auto cmd = frame->GetCmd();

  cmd->beginRendering(renderingInfo);

  //Actual Rendering
  for (const auto scene : GetEngine()->GetScenes()) {
    scene->GetDrawer()->Draw(this, frame);
  }

  cmd->endRendering();
}

void Drawer::CopyImageToImage(const vk::CommandBuffer cmd, const vk::Image src,
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

vk::ImageCreateInfo Drawer::MakeImageCreateInfo(vk::Format format,
                                                vk::Extent3D size,
                                                vk::ImageUsageFlags usage) {
  return {{}, vk::ImageType::e2D,
          format, size, 1,
          1, vk::SampleCountFlagBits::e1,
          vk::ImageTiling::eOptimal,
          usage};
}

vk::ImageViewCreateInfo Drawer::MakeImageViewCreateInfo(vk::Format format,
  vk::Image image, vk::ImageAspectFlags aspect) {
  return {{}, image,
          vk::ImageViewType::e2D,
          format, {},
          {aspect, 0,
           1, 0, 1}};
}

vk::ImageSubresourceRange Drawer::ImageSubResourceRange(
    vk::ImageAspectFlags aspectMask) {
  return {aspectMask, 0, vk::RemainingMipLevels, 0,
          vk::RemainingArrayLayers};
}

vk::Extent2D Drawer::GetSwapchainExtent() const {
  return GetEngine()->GetWindowExtent();
}

vk::Extent2D Drawer::GetSwapchainExtentScaled() const {
  const auto extent = GetSwapchainExtent();

  return {static_cast<uint32_t>(renderScale * extent.width),
          static_cast<uint32_t>(renderScale * extent.height)};
}

vk::Extent2D Drawer::GetDrawImageExtent() const {
  return {static_cast<uint32_t>(_drawImage.extent.width * renderScale),
          static_cast<uint32_t>(_drawImage.extent.height * renderScale)};
}

vk::Format Drawer::GetDrawImageFormat() const {
  return _drawImage.format;
}

vk::Format Drawer::GetDepthImageFormat() const {
  return _depthImage.format;
}

bool Drawer::ResizePending() const {
  return _resizePending;
}

void Drawer::RequestResize() {
  _resizePending = true;
}

void Drawer::CreateComputeShader(const Shader *shader,
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



Engine* Drawer::GetEngine() const {
  return dynamic_cast<Engine *>(GetOuter());
}

vk::Device Drawer::GetDevice() const {
  return _device;
}

vk::PhysicalDevice Drawer::GetPhysicalDevice() const {
  return _gpu;
}

vk::Instance Drawer::GetVulkanInstance() const {
  return _instance;
}

void Drawer::Init(Engine *outer) {
  Object<Engine>::Init(outer);
  vkb::InstanceBuilder builder;

  auto instanceResult =
      builder.set_app_name(GetEngine()->GetAppName().c_str())
             .require_api_version(1, 3, 0)
             .request_validation_layers(true)
#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
            .use_default_debug_messenger()
#endif
      .build();

  auto vkbInstance = instanceResult.value();
  _instance = vkbInstance.instance;

#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
  debugMessenger = vkbInstance.debug_messenger;
#endif

  auto window = GetEngine()->GetWindow();

  VkSurfaceKHR tempSurf;

  SDL_Vulkan_CreateSurface(window, _instance, nullptr, &tempSurf);

  _surface = tempSurf;

  vk::PhysicalDeviceVulkan13Features features;
  features.dynamicRendering = true;
  features.synchronization2 = true;

  vk::PhysicalDeviceVulkan12Features features12;
  features12.bufferDeviceAddress = true;
  features12.descriptorIndexing = true;

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
    _shaderManager->Destroy();

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

  _Allocator = newObject<Allocator>();
  _Allocator->Init(this);
  
  AddCleanup([&] {
    _Allocator->Destroy();
    _Allocator = nullptr;
  });

  InitSwapchain();

  InitCommands();

  //initDefaultRenderPass();

  //initFrameBuffers();

  InitSyncStructures();

  InitDescriptors();

  _shaderManager = newObject<ShaderManager>();
  _shaderManager->Init(this);

  InitPipelineLayout();

  InitPipelines();

  //initImGui();

  InitDefaultTextures();

  InitDefaultMaterials();
}


void Drawer::HandleDestroy() {
  // Wait for the device to idle
  _device.waitIdle();

  Object::HandleDestroy();
}

void Drawer::ImmediateSubmit(
    std::function<void(vk::CommandBuffer cmd)> &&function) {
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

vk::RenderingAttachmentInfo Drawer::MakeRenderingAttachment(
    vk::ImageView view,
    vk::ImageLayout layout, const std::optional<vk::ClearValue> &clear) {
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

AllocatedImage Drawer::CreateImage(const vk::Extent3D size, const vk::Format format,
                                   const vk::ImageUsageFlags usage, const bool mipMapped) const {
  AllocatedImage newImage;
  newImage.format = format;
  newImage.extent = size;

  auto imgInfo = MakeImageCreateInfo(format, size, usage);
  if (mipMapped) {
    imgInfo.setMipLevels(
        static_cast<uint32_t>(std::floor(
            std::log2(std::max(1,1)))) + 1);
  }

  // allocate and create the image
  GetAllocator()->AllocateImage(newImage,imgInfo,VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,vk::MemoryPropertyFlagBits::eDeviceLocal);

  vk::ImageAspectFlags aspectFlags = vk::ImageAspectFlagBits::eColor;
  if (format == vk::Format::eD32Sfloat) {
    aspectFlags = vk::ImageAspectFlagBits::eDepth;
  }

  // Build an image view for the image
  vk::ImageViewCreateInfo viewInfo = MakeImageViewCreateInfo(
      format, newImage.image, aspectFlags);
  viewInfo.subresourceRange.setLevelCount(imgInfo.mipLevels);

  newImage.view = _device.createImageView(viewInfo);

  return newImage;
}

AllocatedImage Drawer::CreateImage(const void *data, const vk::Extent3D size,
                                   const vk::Format format, const vk::ImageUsageFlags usage,
                                   const bool mipMapped) {

  
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
  
  const AllocatedBuffer uploadBuffer = GetAllocator()->CreateTransferCpuGpuBuffer(dataSize, false);
  const auto mapped = uploadBuffer.alloc.GetMappedData();
  utils::vassert(mapped != nullptr && data != nullptr,"WE DONE FUCKED UP");
  memcpy(mapped, data, dataSize);

  const AllocatedImage newImage = CreateImage(size, format,
                                              usage |
                                              vk::ImageUsageFlagBits::eTransferDst
                                              | vk::ImageUsageFlagBits::eTransferSrc,
                                              mipMapped);


  
  ImmediateSubmit([&](const vk::CommandBuffer cmd) {
    TransitionImage(cmd, newImage.image, vk::ImageLayout::eUndefined,
                    vk::ImageLayout::eTransferDstOptimal);

    vk::BufferImageCopy copyRegion{0, 0, 0};
    copyRegion.setImageSubresource({vk::ImageAspectFlagBits::eColor, 0, 0, 1});
    copyRegion.setImageExtent(size);

    cmd.copyBufferToImage(uploadBuffer.buffer, newImage.image,
                          vk::ImageLayout::eTransferDstOptimal, 1, &copyRegion);

    TransitionImage(cmd, newImage.image, vk::ImageLayout::eTransferDstOptimal,
                    vk::ImageLayout::eShaderReadOnlyOptimal);
  });

  GetAllocator()->DestroyBuffer(uploadBuffer);

  return newImage;
}


GpuMeshBuffers Drawer::CreateMeshBuffers(const Mesh *mesh) {
  const auto vertices = mesh->GetVertices();
  const auto indices = mesh->GetIndices();
  const auto vertexBufferSize = vertices.ByteSize();
  const auto indexBufferSize = indices.ByteSize();

  GpuMeshBuffers newBuffers;

  newBuffers.vertexBuffer = GetAllocator()->CreateBuffer(vertexBufferSize,
                                         vk::BufferUsageFlagBits::eStorageBuffer
                                         | vk::BufferUsageFlagBits::eTransferDst
                                         | vk::BufferUsageFlagBits::eShaderDeviceAddress,
                                         VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,
                                         vk::MemoryPropertyFlagBits::eDeviceLocal);

  const vk::BufferDeviceAddressInfo deviceAddressInfo{
      newBuffers.vertexBuffer.buffer};
  newBuffers.vertexBufferAddress = _device.getBufferAddress(deviceAddressInfo);

  newBuffers.indexBuffer = GetAllocator()->CreateBuffer(vertexBufferSize,
                                        vk::BufferUsageFlagBits::eIndexBuffer
                                        | vk::BufferUsageFlagBits::eTransferDst,
                                        VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,
                                        vk::MemoryPropertyFlagBits::eDeviceLocal);

  const auto stagingBuffer = GetAllocator()->CreateTransferCpuGpuBuffer(
      vertexBufferSize + indexBufferSize, false);
  
  const auto data = stagingBuffer.alloc.GetMappedData();
  memcpy(data, vertices.data(), vertexBufferSize);
  memcpy(static_cast<char *>(data) + vertexBufferSize, indices.data(),
         indexBufferSize);

  ImmediateSubmit([=](const vk::CommandBuffer cmd) {
    const vk::BufferCopy vertexCopy{0, 0, vertexBufferSize};

    cmd.copyBuffer(stagingBuffer.buffer, newBuffers.vertexBuffer.buffer, 1,
                   &vertexCopy);

    const vk::BufferCopy indicesCopy{vertexBufferSize, 0, indexBufferSize};

    cmd.copyBuffer(stagingBuffer.buffer, newBuffers.indexBuffer.buffer, 1,
                   &indicesCopy);
  });

  GetAllocator()->DestroyBuffer(stagingBuffer);
  return newBuffers;
}

Allocator *Drawer::GetAllocator() const {
  return _Allocator;
}

Texture * Drawer::GetDefaultWhiteTexture() const {
  return _whiteTexture;
}

Texture * Drawer::GetDefaultBlackTexture() const {
  return _blackTexture;
}

Texture * Drawer::GetDefaultGreyTexture() const {
  return _greyTexture;
}

Texture * Drawer::GetDefaultErrorCheckerboardTexture() const {
  return _errorCheckerboardTexture;
}

// void Drawer::onResize(const std::function<void()> &callback) {
//   _resizeCallbacks.push_back(callback);
// }

vk::Sampler Drawer::GetDefaultSamplerLinear() const {
  return _defaultSamplerLinear;
}

vk::Sampler Drawer::GetDefaultSamplerNearest() const {
  return _defaultSamplerNearest;
}

MaterialInstance * Drawer::GetDefaultCheckeredMaterial() const {
  return _defaultCheckeredMaterial;
}

ShaderManager *Drawer::GetShaderManager() const {
  return _shaderManager;
}

vk::DescriptorSetLayout Drawer::GetSceneDescriptorLayout() const {
  return _sceneDescriptorSetLayout;
}

DescriptorAllocatorGrowable * Drawer::GetGlobalDescriptorAllocator() {
  return &_globalAllocator;
}

void Drawer::Draw() {

  const auto frame = GetCurrentFrame();
  // Wait for gpu to finish past work
  vk::resultCheck(
      _device.waitForFences({frame->GetRenderFence()}, true, 1000000000),
      "Wait For Fences Failed");

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
    _resizePending = true;
    return;
  }

  const auto cmd = frame->GetCmd();

  // Clear command buffer and prepare to render
  cmd->reset();

  constexpr auto commandBeginInfo = vk::CommandBufferBeginInfo(
      vk::CommandBufferUsageFlagBits::eOneTimeSubmit);

  const auto swapchainExtent = GetSwapchainExtent();

  const vk::Extent2D drawExtent = GetDrawImageExtent();

  cmd->begin(commandBeginInfo);

  // Transition image to general layout
  TransitionImage(*cmd, _drawImage.image,
                  vk::ImageLayout::eUndefined, vk::ImageLayout::eGeneral);

  DrawBackground(frame);

  TransitionImage(*cmd, _drawImage.image,
                  vk::ImageLayout::eGeneral,
                  vk::ImageLayout::eColorAttachmentOptimal);
  TransitionImage(*cmd, _depthImage.image,
                  vk::ImageLayout::eUndefined,
                  vk::ImageLayout::eDepthAttachmentOptimal);

  DrawScenes(frame);

  // Transition images to correct transfer layouts
  TransitionImage(*cmd, _drawImage.image,
                  vk::ImageLayout::eColorAttachmentOptimal,
                  vk::ImageLayout::eTransferSrcOptimal);
  TransitionImage(*cmd, _swapchainImages[swapchainImageIndex],
                  vk::ImageLayout::eUndefined,
                  vk::ImageLayout::eTransferDstOptimal);

  CopyImageToImage(*cmd, _drawImage.image,
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

  _graphicsQueue.submit2({submitInfo}, frame->GetRenderFence());

  const auto renderSemaphore = frame->GetRenderSemaphore();
  const auto presentInfo = vk::PresentInfoKHR({renderSemaphore},
                                              {_swapchain},
                                              swapchainImageIndex);

  try {
    const auto _ = _graphicsQueue.presentKHR(presentInfo);
  } catch (vk::OutOfDateKHRError &_) {
    _resizePending = true;
    return;
  }

  _frameCount++;
}
} // namespace rendering
} // namespace vengine