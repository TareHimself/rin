#define VMA_IMPLEMENTATION
#include "Drawer.hpp"

#include "Material.hpp"
#include "Mesh.hpp"
#include "PipelineBuilder.hpp"
#include "Shader.hpp"
#include "vengine/Engine.hpp"
#include "vengine/io/io.hpp"
#include <VkBootstrap.h>
#include <SDL3/SDL.h>
#include <SDL3/SDL_vulkan.h>
#include "types.hpp"
#include "vengine/scene/Scene.hpp"
#include <vk_mem_alloc.hpp>
#include "imgui.h"
#include "imgui_impl_sdl3.h"
#include "imgui_impl_vulkan.h"
#include "scene/SceneDrawer.hpp"


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

void Drawer::initSwapchain() {
  const auto extent = getSwapchainExtent();

  createSwapchain();

  addCleanup([=] {
    if (_swapchain != nullptr) {
      destroySwapchain();
    }
  });

  vk::ImageUsageFlags drawImageUsages;
  drawImageUsages |= vk::ImageUsageFlagBits::eTransferSrc;
  drawImageUsages |= vk::ImageUsageFlagBits::eTransferDst;
  drawImageUsages |= vk::ImageUsageFlagBits::eStorage;
  drawImageUsages |= vk::ImageUsageFlagBits::eColorAttachment;

  allocateImage(_drawImage, vk::Format::eR16G16B16A16Sfloat,
                vk::Extent3D{extent.width, extent.height, 1}, drawImageUsages,
                vk::ImageAspectFlagBits::eColor, vma::MemoryUsage::eGpuOnly,
                vk::MemoryPropertyFlagBits::eDeviceLocal);

  allocateImage(_depthImage, vk::Format::eD32Sfloat, _drawImage.extent,
                vk::ImageUsageFlagBits::eDepthStencilAttachment,
                vk::ImageAspectFlagBits::eDepth, vma::MemoryUsage::eGpuOnly,
                vk::MemoryPropertyFlagBits::eDeviceLocal);

  addCleanup([=] {
    _device.destroyImageView(_drawImage.view);
    vmaDestroyImage(getAllocator(), _drawImage.image, _drawImage.alloc);

    _device.destroyImageView(_depthImage.view);
    vmaDestroyImage(getAllocator(), _depthImage.image, _depthImage.alloc);
  });
}

void Drawer::createSwapchain() {
  const auto extent = getSwapchainExtent();

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
}

void Drawer::resizeSwapchain() {
  log::drawing->info("Resizing Swapchain");
  _device.waitIdle();

  destroySwapchain();
  log::drawing->info("Destroyed Old Swapchain");

  getEngine()->notifyWindowResize();

  createSwapchain();
  log::drawing->info("Created New Swapchain");

  _resizePending = false;

  log::drawing->info("Swapchain Resize Completed");
}

void Drawer::destroySwapchain() {
  for (const auto view : _swapchainImageViews) {
    _device.destroyImageView(view);
  }

  _swapchainImageViews.clear();
  _swapchainImages.clear();

  _device.destroySwapchainKHR(_swapchain);

  _swapchain = nullptr;
}

void Drawer::initCommands() {
  const auto commandPoolInfo = vk::CommandPoolCreateInfo(
      vk::CommandPoolCreateFlags(
          vk::CommandPoolCreateFlagBits::eResetCommandBuffer),
      _graphicsQueueFamily);

  for (auto &_frame : _frames) {

    _frame.setCommandPool(_device.createCommandPool(commandPoolInfo, nullptr));

    const auto commandBufferAllocateInfo = vk::CommandBufferAllocateInfo(
        *_frame.getCmdPool(), vk::CommandBufferLevel::ePrimary, 1);

    _frame.setCommandBuffer(
        _device.allocateCommandBuffers(commandBufferAllocateInfo)
               .
               at(0));
  }

  _immCommandPool = _device.createCommandPool(commandPoolInfo, nullptr);

  const vk::CommandBufferAllocateInfo cmdAllocInfo{
      _immCommandPool, vk::CommandBufferLevel::ePrimary, 1};

  _immediateCommandBuffer = _device.allocateCommandBuffers(cmdAllocInfo).at(0);

  addCleanup([this] {
    _device.destroyCommandPool(_immCommandPool);

    for (auto &_frame : _frames) {
      _device.destroyCommandPool(*_frame.getCmdPool());
    }
  });
}


void Drawer::initSyncStructures() {
  constexpr auto fenceCreateInfo = vk::FenceCreateInfo(
      vk::FenceCreateFlagBits::eSignaled);

  constexpr auto semaphoreCreateInfo = vk::SemaphoreCreateInfo(
      vk::SemaphoreCreateFlags());

  for (auto &_frame : _frames) {
    _frame.setRenderFence(_device.createFence(fenceCreateInfo));
    _frame.setSemaphores(_device.createSemaphore(semaphoreCreateInfo),
                         _device.createSemaphore(semaphoreCreateInfo));
  }

  _immediateFence = _device.createFence(fenceCreateInfo);

  addCleanup([this] {

    _device.destroyFence(_immediateFence);

    for (const auto &_frame : _frames) {
      _device.destroyFence(_frame.getRenderFence());
      _device.destroySemaphore(_frame.getRenderSemaphore());
      _device.destroySemaphore(_frame.getSwapchainSemaphore());
    }
  });

}

void Drawer::initPipelineLayout() {
  auto pushConstants = {
      vk::PushConstantRange({vk::ShaderStageFlagBits::eVertex}, 0,
                            sizeof(GpuDrawPushConstants))};
  _mainPipelineLayout = _device.createPipelineLayout(
      vk::PipelineLayoutCreateInfo({}, {}, pushConstants));
  addCleanup([this] {
    _device.destroyPipelineLayout(_mainPipelineLayout);
  });
}

void Drawer::initPipelines() {
  PipelineBuilder builder;
  _mainPipeline = builder
                  .setLayout(_mainPipelineLayout)
                  .addVertexShader(Shader::fromSource(
                      _shaderManager, io::getRawShaderPath("triangle.vert")))
                  .addFragmentShader(Shader::fromSource(
                      _shaderManager, io::getRawShaderPath("triangle.frag")))
                  .setInputTopology(vk::PrimitiveTopology::eTriangleList)
                  .setPolygonMode(vk::PolygonMode::eFill)
                  .setCullMode(vk::CullModeFlagBits::eNone,
                               vk::FrontFace::eClockwise)
                  .setMultisamplingModeNone()
                  .disableBlending()
                  //.disableDepthTest()
                  .enableDepthTest(true, vk::CompareOp::eLessOrEqual)
                  .setColorAttachmentFormat(_drawImage.format)
                  .setDepthFormat(_depthImage.format)
                  .build(_device);

  ComputeEffect gradient{"gradient"};
  ComputeEffect gradient2{"gradient 2"};

  createComputeShader(
      Shader::fromSource(_shaderManager, io::getRawShaderPath("pretty.comp")),
      gradient2);
  createComputeShader(
      Shader::fromSource(_shaderManager, io::getRawShaderPath("pretty.comp")),
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
  backgroundEffects.push(gradient);
  backgroundEffects.push(gradient2);

  addCleanup([this] {
    for (const auto &effect : backgroundEffects) {
      _device.destroyPipeline(effect.pipeline);
      _device.destroyPipelineLayout(effect.layout);
    }

    _device.destroyPipeline(_mainPipeline);
  });
}

void Drawer::initDescriptors() {
  // 10 sets 1 image each

  {
    const auto device = getDevice();
    DescriptorLayoutBuilder builder;
    builder.addBinding(0, vk::DescriptorType::eUniformBuffer);
    _sceneDescriptorSetLayout = builder.build(device,
                                              vk::ShaderStageFlagBits::eVertex
                                              | vk::ShaderStageFlagBits::eFragment);

    addCleanup([=] {
      device.destroyDescriptorSetLayout(_sceneDescriptorSetLayout);
    });
  }

  Array<DescriptorAllocatorGrowable::PoolSizeRatio> sizes = {
      {vk::DescriptorType::eStorageImage, 1}};

  _descriptorAllocator.init(_device, 10, sizes);

  addCleanup([=] {
    _descriptorAllocator.destroyPools();
  });

  {
    DescriptorLayoutBuilder builder;
    builder.addBinding(0, vk::DescriptorType::eStorageImage);
    _drawImageDescriptorLayout = builder.build(_device,
                                               vk::ShaderStageFlagBits::eCompute);

    addCleanup([=] {
      _device.destroyDescriptorSetLayout(_drawImageDescriptorLayout);
      //descriptorAllocator.
    });
  }

  _drawImageDescriptors = _descriptorAllocator.allocate(
      _drawImageDescriptorLayout);

  DescriptorWriter writer;
  writer.writeImage(0, _drawImage.view, {}, vk::ImageLayout::eGeneral,
                    vk::DescriptorType::eStorageImage);

  writer.updateSet(_device, _drawImageDescriptors);

  for (auto &_frame : _frames) {
    // create a descriptor pool
    std::vector<DescriptorAllocatorGrowable::PoolSizeRatio> frame_sizes = {
        {vk::DescriptorType::eStorageImage, 3},
        {vk::DescriptorType::eStorageBuffer, 3},
        {vk::DescriptorType::eUniformBuffer, 3},
        {vk::DescriptorType::eCombinedImageSampler, 4},
    };

    _frame.getDescriptorAllocator()->init(_device, 1000, frame_sizes);

    addCleanup([&] {
      _frame.getDescriptorAllocator()->destroyPools();
      _frame.cleaner.run();
    });
  }
}

void Drawer::initImGui() {
  constexpr auto poolSize = 1000;
  Array<vk::DescriptorPoolSize> poolSizes = {
      {vk::DescriptorType::eSampler, poolSize},
      {vk::DescriptorType::eCombinedImageSampler, poolSize},
      {vk::DescriptorType::eSampledImage, poolSize},
      {vk::DescriptorType::eStorageImage, poolSize},
      {vk::DescriptorType::eUniformTexelBuffer, poolSize},
      {vk::DescriptorType::eStorageTexelBuffer, poolSize},
      {vk::DescriptorType::eUniformBuffer, poolSize},
      {vk::DescriptorType::eStorageBuffer, poolSize},
      {vk::DescriptorType::eUniformBufferDynamic, poolSize},
      {vk::DescriptorType::eStorageBufferDynamic, poolSize},
      {vk::DescriptorType::eInputAttachment, poolSize}
  };

  const vk::DescriptorPoolCreateInfo poolInfo{
      vk::DescriptorPoolCreateFlagBits::eFreeDescriptorSet, poolSize,
      poolSizes};

  const vk::DescriptorPool imGuiPool = _device.createDescriptorPool(poolInfo);

  ImGui::CreateContext();

  ImGui_ImplSDL3_InitForVulkan(getEngine()->getWindow());

  ImGui_ImplVulkan_InitInfo initInfo{instance, _gpu, _device,
                                     _graphicsQueueFamily, _graphicsQueue,
                                     nullptr, imGuiPool};
  initInfo.MinImageCount = 3;
  initInfo.ImageCount = 3;
  initInfo.UseDynamicRendering = true;
  initInfo.ColorAttachmentFormat = static_cast<VkFormat>(_swapchainImageFormat);

  initInfo.MSAASamples = static_cast<VkSampleCountFlagBits>(
    vk::SampleCountFlagBits::e1);

  ImGui_ImplVulkan_Init(&initInfo, nullptr);

  // immediateSubmit([&] (vk::CommandBuffer cmd){
  //   ImGui_ImplVulkan_CreateFontsTexture();
  // });

  addCleanup([=] {
    ImGui_ImplVulkan_Shutdown();
    ImGui_ImplSDL3_Shutdown();
    _device.destroyDescriptorPool(imGuiPool);
  });
}

void Drawer::initDefaultTextures() {
  //3 default textures, white, grey, black. 1 pixel each
  uint32_t white = 0xFFFFFFFF;
  _whiteImage = createImage((void *)&white, vk::Extent3D{1, 1, 1},
                            vk::Format::eR8G8B8A8Unorm,
                            vk::ImageUsageFlagBits::eSampled);

  uint32_t grey = 0xAAAAAAFF;
  _greyImage = createImage((void *)&grey, vk::Extent3D{1, 1, 1},
                           vk::Format::eR8G8B8A8Unorm,
                           vk::ImageUsageFlagBits::eSampled);

  uint32_t black = 0x000000FF;
  _blackImage = createImage((void *)&black, vk::Extent3D{1, 1, 1},
                            vk::Format::eR8G8B8A8Unorm,
                            vk::ImageUsageFlagBits::eSampled);

  //checkerboard image
  uint32_t magenta = 0xFF00FFFF;
  std::array<uint32_t, 16 * 16> pixels; //for 16x16 checkerboard texture
  for (int x = 0; x < 16; x++) {
    for (int y = 0; y < 16; y++) {
      pixels[y * 16 + x] = ((x % 2) ^ (y % 2)) ? magenta : black;
    }
  }

  _errorCheckerboardImage = createImage(pixels.data(), vk::Extent3D{16, 16, 1},
                                        vk::Format::eR8G8B8A8Unorm,
                                        vk::ImageUsageFlagBits::eSampled);

  vk::SamplerCreateInfo samplerInfo;
  samplerInfo.setMagFilter(vk::Filter::eNearest);
  samplerInfo.setMinFilter(vk::Filter::eNearest);

  _defaultSamplerNearest = _device.createSampler(samplerInfo);

  samplerInfo.setMagFilter(vk::Filter::eLinear);
  samplerInfo.setMinFilter(vk::Filter::eLinear);

  _defaultSamplerLinear = _device.createSampler(samplerInfo);
}

void Drawer::initDefaultMaterials() {
  Material::MaterialResources materialResources;
  materialResources.color = _errorCheckerboardImage;
  materialResources.colorSampler = _defaultSamplerLinear;
  materialResources.metallic = _whiteImage;
  materialResources.metallicSampler = _defaultSamplerLinear;

  const auto materialConstants = createUniformCpuGpuBuffer(sizeof(Material::MaterialConstants),true);

  const auto sceneUniformData = static_cast<Material::MaterialConstants *>(materialConstants.info.pMappedData);
  sceneUniformData->colorFactors = glm::vec4{1,1,1,1};
  sceneUniformData->metalRoughFactors = glm::vec4{1,0.5,0,0};

  addCleanup([=,this] {
    destroyBuffer(materialConstants);
  });

  materialResources.dataBuffer = materialConstants.buffer;
  materialResources.dataBufferOffset = 0;

  _defaultCheckeredMaterial = Material::create(this,EMaterialPass::Opaque,_descriptorAllocator,materialResources);
}

void Drawer::transitionImage(vk::CommandBuffer cmd, vk::Image image,
                             vk::ImageLayout currentLayout,
                             vk::ImageLayout newLayout) {
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
  imageBarrier.setSubresourceRange(imageSubResourceRange(aspectMask));

  imageBarrier.setImage(image);

  vk::DependencyInfo depInfo;
  depInfo.setImageMemoryBarriers({imageBarrier});

  cmd.pipelineBarrier2(&depInfo);
}

vk::RenderingInfo Drawer::makeRenderingInfo(vk::Extent2D drawExtent) {
  return vk::RenderingInfo({}, {{0, 0}, drawExtent}, 1, {});
}

FrameData *Drawer::getCurrentFrame() {
  return &_frames[frameCount % FRAME_OVERLAP];
}

void Drawer::drawBackground(FrameData *frame) {
  const auto cmd = frame->getCmd();

  float flash = abs(sin(frameCount / 120.f));

  const auto clearValue = vk::ClearColorValue({0.0f, 0.0f, flash, 0.0f});

  auto clearRange = imageSubResourceRange(vk::ImageAspectFlagBits::eColor);

  cmd->clearColorImage(_drawImage.image,
                       vk::ImageLayout::eGeneral, clearValue,
                       {clearRange});
  auto computeEffect = backgroundEffects.at(currentBackgroundEffect);
  computeEffect.data.time = getEngine()->getEngineTimeSeconds();
  cmd->bindPipeline(vk::PipelineBindPoint::eCompute, computeEffect.pipeline);

  cmd->bindDescriptorSets(vk::PipelineBindPoint::eCompute,
                          computeEffect.layout, 0, {_drawImageDescriptors},
                          {});

  cmd->pushConstants(computeEffect.layout, vk::ShaderStageFlagBits::eCompute, 0,
                     computeEffect.size, &computeEffect.data);
  const auto extent = getEngine()->getWindowExtent();

  cmd->dispatch(std::ceil(extent.width / 16.0), std::ceil(extent.height / 16.0),
                1);
}

void Drawer::drawScenes(FrameData *frame) {
  const vk::Extent2D drawExtent = getDrawImageExtent();

  const auto colorAttachment = makeRenderingAttachment(
      _drawImage.view, vk::ImageLayout::eGeneral);

  vk::ClearValue depthClear;
  depthClear.setDepthStencil({1.f});

  const auto depthAttachment = makeRenderingAttachment(
      _depthImage.view, vk::ImageLayout::eDepthAttachmentOptimal, depthClear);

  auto renderingInfo = makeRenderingInfo(drawExtent);
  renderingInfo.setColorAttachments(colorAttachment);
  renderingInfo.setPDepthAttachment(&depthAttachment);

  const auto cmd = frame->getCmd();

  cmd->beginRendering(renderingInfo);
  // cmd->bindPipeline(vk::PipelineBindPoint::eGraphics, _mainPipeline);

  //Actual Rendering
  for (const auto scene : getEngine()->getScenes()) {
    scene->getDrawer()->draw(this, frame);
  }

  cmd->endRendering();
}

void Drawer::copyImageToImage(vk::CommandBuffer cmd, vk::Image src,
                              vk::Image dst, vk::Extent2D srcSize,
                              vk::Extent2D dstSize) {
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

vk::ImageCreateInfo Drawer::makeImageCreateInfo(vk::Format format,
                                                vk::Extent3D size,
                                                vk::ImageUsageFlags usage) {
  return {{}, vk::ImageType::e2D,
          format, size, 1,
          1, vk::SampleCountFlagBits::e1,
          vk::ImageTiling::eOptimal,
          usage};
}

vk::ImageViewCreateInfo Drawer::makeImageViewCreateInfo(vk::Format format,
  vk::Image image, vk::ImageAspectFlags aspect) {
  return {{}, image,
          vk::ImageViewType::e2D,
          format, {},
          {aspect, 0,
           1, 0, 1}};
}

vk::ImageSubresourceRange Drawer::imageSubResourceRange(
    vk::ImageAspectFlags aspectMask) {
  return {aspectMask, 0, vk::RemainingMipLevels, 0,
          vk::RemainingArrayLayers};
}

vk::Extent2D Drawer::getSwapchainExtent() const {
  return getEngine()->getWindowExtent();
}

vk::Extent2D Drawer::getSwapchainExtentScaled() const {
  const auto extent = getSwapchainExtent();

  return {static_cast<uint32_t>(extent.width * renderScale),
          static_cast<uint32_t>(extent.height * renderScale)};
}

vk::Extent2D Drawer::getDrawImageExtent() const {
  const auto swapchainExtent = getSwapchainExtent();
  return {static_cast<uint32_t>(renderScale * std::min(
                                    _drawImage.extent.width,
                                    swapchainExtent.width)),
          static_cast<uint32_t>(renderScale * std::min(
                                    _drawImage.extent.height,
                                    swapchainExtent.height))};
}

vk::Format Drawer::getDrawImageFormat() const {
  return _drawImage.format;
}

vk::Format Drawer::getDepthImageFormat() const {
  return _depthImage.format;
}

bool Drawer::resizePending() const {
  return _resizePending;
}

void Drawer::createComputeShader(const Shader *shader,
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

void Drawer::allocateImage(AllocatedImage &image, vk::Format format,
                           vk::Extent3D extent, vk::ImageUsageFlags usage,
                           vk::ImageAspectFlags aspectFlags,
                           vma::MemoryUsage memoryUsage,
                           vk::MemoryPropertyFlags requiredFlags) const {
  image.format = format;
  image.extent = extent;

  const auto imageCreateInfo = makeImageCreateInfo(
      image.format, image.extent, usage);

  vma::AllocationCreateInfo imageAllocInfo = {};
  imageAllocInfo.setUsage(memoryUsage);
  imageAllocInfo.setRequiredFlags(requiredFlags);

  vk::resultCheck(
      getAllocator().createImage(&imageCreateInfo, &imageAllocInfo,
                                 &image.image, &image.alloc, nullptr),
      "Failed to allocate image");

  const auto viewInfo = vk::ImageViewCreateInfo({}, image.image,
                                                vk::ImageViewType::e2D,
                                                image.format, {},
                                                {aspectFlags, 0,
                                                 1, 0, 1});

  image.view = _device.createImageView(viewInfo);
}

Engine *Drawer::getEngine() const {
  return dynamic_cast<Engine *>(getOuter());
}

vk::Device Drawer::getDevice() const {
  return _device;
}

void Drawer::init(Engine *outer) {
  Object<Engine>::init(outer);
  vkb::InstanceBuilder builder;

  auto instanceResult =
      builder.set_app_name(getEngine()->getAppName().c_str())
             .require_api_version(1, 3, 0)
             .request_validation_layers(true)
#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
            .use_default_debug_messenger()
#endif
      .build();

  auto vkbInstance = instanceResult.value();
  instance = vkbInstance.instance;

#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
  debugMessenger = vkbInstance.debug_messenger;
#endif

  auto window = getEngine()->getWindow();

  VkSurfaceKHR tempSurf;

  SDL_Vulkan_CreateSurface(window, instance, nullptr, &tempSurf);

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

  addCleanup([this] {
    _shaderManager->cleanup();

    _device.destroy();
    instance.destroySurfaceKHR(_surface);

#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
instance.destroyDebugUtilsMessengerEXT(debugMessenger);
#endif

    instance.destroy();
  });

  auto allocatorCreateInfo = vma::AllocatorCreateInfo{
      vma::AllocatorCreateFlagBits::eBufferDeviceAddress, _gpu, _device};
  allocatorCreateInfo.setInstance(instance);
  _vkAllocator = vma::createAllocator(allocatorCreateInfo);

  addCleanup([&] {
    _vkAllocator.destroy();
    _vkAllocator = nullptr;
  });

  initSwapchain();

  initCommands();

  //initDefaultRenderPass();

  //initFrameBuffers();

  initSyncStructures();

  initDescriptors();

  _shaderManager = newObject<ShaderManager>();
  _shaderManager->init(this);

  initPipelineLayout();

  initPipelines();

  initImGui();

  initDefaultTextures();

  initDefaultMaterials();
}


void Drawer::handleCleanup() {
  // Wait for the device to idle
  _device.waitIdle();

  Object::handleCleanup();
}

void Drawer::immediateSubmit(
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

vk::RenderingAttachmentInfo Drawer::makeRenderingAttachment(
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


void Drawer::drawImGui(vk::CommandBuffer cmd, vk::ImageView view) {
  auto colorAttachment = makeRenderingAttachment(
      view, vk::ImageLayout::eGeneral);
  const auto extent = getSwapchainExtent();

  const auto renderInfo = vk::RenderingInfo({}, {{0, 0}, extent}, 1,
                                            {}, {colorAttachment});

  cmd.beginRendering(renderInfo);

  ImGui_ImplVulkan_RenderDrawData(ImGui::GetDrawData(), cmd);

  cmd.endRendering();
}

AllocatedBuffer Drawer::createBuffer(size_t allocSize,
                                     vk::BufferUsageFlags usage,
                                     vma::MemoryUsage memoryUsage,
                                     vk::MemoryPropertyFlags requiredFlags,
                                     vma::AllocationCreateFlags flags) {
  const auto bufferInfo = vk::BufferCreateInfo({}, allocSize,
                                               usage);
  //vma::AllocationCreateFlagBits::eMapped
  const auto vmaAllocInfo = vma::AllocationCreateInfo(
      flags, memoryUsage, requiredFlags);

  AllocatedBuffer newBuffer;

  vk::resultCheck(
      getAllocator().createBuffer(&bufferInfo, &vmaAllocInfo, &newBuffer.buffer,
                                  &newBuffer.alloc, &newBuffer.info),
      "Failed to create buffer");

  return newBuffer;
}

AllocatedBuffer Drawer::createTransferCpuGpuBuffer(
    size_t size, bool randomAccess) {
  return createBuffer(size,
                      vk::BufferUsageFlagBits::eTransferSrc,
                      vma::MemoryUsage::eAutoPreferHost,
                      vk::MemoryPropertyFlagBits::eHostVisible |
                      vk::MemoryPropertyFlagBits::eHostCoherent,
                      vma::AllocationCreateFlagBits::eMapped | (randomAccess
                        ? vma::AllocationCreateFlagBits::eHostAccessRandom
                        : vma::AllocationCreateFlagBits::eHostAccessSequentialWrite));
}

AllocatedBuffer Drawer::createUniformCpuGpuBuffer(size_t size,
                                                  bool randomAccess) {
  return createBuffer(
      size, vk::BufferUsageFlagBits::eUniformBuffer,
      vma::MemoryUsage::eAutoPreferDevice,
      vk::MemoryPropertyFlagBits::eHostVisible,
      vma::AllocationCreateFlagBits::eMapped |
      (randomAccess
         ? vma::AllocationCreateFlagBits::eHostAccessRandom
         : vma::AllocationCreateFlagBits::eHostAccessSequentialWrite));
}

void Drawer::destroyBuffer(const AllocatedBuffer &buffer) {
  getAllocator().destroyBuffer(buffer.buffer, buffer.alloc);
}

AllocatedImage Drawer::createImage(vk::Extent3D size, vk::Format format,
                                   vk::ImageUsageFlags usage, bool mipMapped) {
  AllocatedImage newImage;
  newImage.format = format;
  newImage.extent = size;

  auto imgInfo = makeImageCreateInfo(format, size, usage);
  if (mipMapped) {
    imgInfo.setMipLevels(
        static_cast<uint32_t>(std::floor(
            std::log2(std::max(size.width, size.height)))) + 1);
  }

  // Allocate Image on Dedicated GPU Memory
  constexpr vma::AllocationCreateInfo allocInfo{
      {}, vma::MemoryUsage::eAutoPreferDevice,
      vk::MemoryPropertyFlagBits::eDeviceLocal};

  // allocate and create the image
  vk::resultCheck(
      getAllocator().createImage(&imgInfo, &allocInfo, &newImage.image,
                                 &newImage.alloc, nullptr),
      "Failed to allocate image");

  vk::ImageAspectFlags aspectFlags = vk::ImageAspectFlagBits::eColor;
  if (format == vk::Format::eD32Sfloat) {
    aspectFlags = vk::ImageAspectFlagBits::eDepth;
  }

  // Build an image view for the image
  vk::ImageViewCreateInfo viewInfo = makeImageViewCreateInfo(
      format, newImage.image, aspectFlags);
  viewInfo.subresourceRange.setLevelCount(imgInfo.mipLevels);

  newImage.view = _device.createImageView(viewInfo);

  return newImage;
}

AllocatedImage Drawer::createImage(void *data, vk::Extent3D size,
                                   vk::Format format, vk::ImageUsageFlags usage,
                                   bool mipMapped) {

  const auto dataSize = size.depth * size.width * size.height * 4;

  const AllocatedBuffer uploadBuffer =
      createTransferCpuGpuBuffer(dataSize, true);

  memcpy(uploadBuffer.info.pMappedData, data, dataSize);

  const AllocatedImage newImage = createImage(size, format,
                                              usage |
                                              vk::ImageUsageFlagBits::eTransferDst
                                              | vk::ImageUsageFlagBits::eTransferSrc,
                                              mipMapped);

  immediateSubmit([&](vk::CommandBuffer cmd) {
    transitionImage(cmd, newImage.image, vk::ImageLayout::eUndefined,
                    vk::ImageLayout::eTransferDstOptimal);

    vk::BufferImageCopy copyRegion{0, 0, 0};
    copyRegion.setImageSubresource({vk::ImageAspectFlagBits::eColor, 0, 0, 1});
    copyRegion.setImageExtent(size);

    cmd.copyBufferToImage(uploadBuffer.buffer, newImage.image,
                          vk::ImageLayout::eTransferDstOptimal, 1, &copyRegion);

    transitionImage(cmd, newImage.image, vk::ImageLayout::eTransferDstOptimal,
                    vk::ImageLayout::eShaderReadOnlyOptimal);
  });

  destroyBuffer(uploadBuffer);

  return newImage;
}

void Drawer::destroyImage(const AllocatedImage &image) {
  _device.destroyImageView(image.view);
  getAllocator().destroyImage(image.image, image.alloc);
}

GpuMeshBuffers Drawer::createMeshBuffers(const Mesh *mesh) {
  const auto vertices = mesh->getVertices();
  const auto indices = mesh->getIndices();
  const auto vertexBufferSize = vertices.byteSize();
  const auto indexBufferSize = indices.byteSize();

  GpuMeshBuffers newSurface;

  newSurface.vertexBuffer = createBuffer(vertexBufferSize,
                                         vk::BufferUsageFlagBits::eStorageBuffer
                                         | vk::BufferUsageFlagBits::eTransferDst
                                         | vk::BufferUsageFlagBits::eShaderDeviceAddress,
                                         vma::MemoryUsage::eAutoPreferDevice,
                                         vk::MemoryPropertyFlagBits::eDeviceLocal);

  const vk::BufferDeviceAddressInfo deviceAddressInfo{
      newSurface.vertexBuffer.buffer};
  newSurface.vertexBufferAddress = _device.getBufferAddress(deviceAddressInfo);

  newSurface.indexBuffer = createBuffer(vertexBufferSize,
                                        vk::BufferUsageFlagBits::eIndexBuffer
                                        | vk::BufferUsageFlagBits::eTransferDst,
                                        vma::MemoryUsage::eAutoPreferDevice,
                                        vk::MemoryPropertyFlagBits::eDeviceLocal);

  const auto stagingBuffer = createTransferCpuGpuBuffer(
      vertexBufferSize + indexBufferSize, false);

  //const auto data = stagingBuffer.info.pMappedData;
  const VmaAllocation alloc = stagingBuffer.alloc;
  const auto data = alloc->GetMappedData();
  memcpy(data, vertices.data(), vertexBufferSize);
  memcpy(static_cast<char *>(data) + vertexBufferSize, indices.data(),
         indexBufferSize);

  immediateSubmit([=](vk::CommandBuffer cmd) {
    const vk::BufferCopy vertexCopy{0, 0, vertexBufferSize};

    cmd.copyBuffer(stagingBuffer.buffer, newSurface.vertexBuffer.buffer, 1,
                   &vertexCopy);

    const vk::BufferCopy indicesCopy{vertexBufferSize, 0, indexBufferSize};

    cmd.copyBuffer(stagingBuffer.buffer, newSurface.indexBuffer.buffer, 1,
                   &indicesCopy);
  });

  destroyBuffer(stagingBuffer);

  addCleanup([=] {
    destroyBuffer(newSurface.indexBuffer);
    destroyBuffer(newSurface.vertexBuffer);
  });

  return newSurface;
}

vma::Allocator Drawer::getAllocator() const {
  return _vkAllocator;
}

AllocatedImage Drawer::getDefaultWhiteImage() const {
  return _whiteImage;
}

AllocatedImage Drawer::getDefaultBlackImage() const {
  return _blackImage;
}

AllocatedImage Drawer::getDefaultGreyImage() const {
  return _greyImage;
}

AllocatedImage Drawer::getDefaultErrorCheckerboardImage() const {
  return _errorCheckerboardImage;
}

vk::Sampler Drawer::getDefaultSamplerLinear() const {
  return _defaultSamplerLinear;
}

vk::Sampler Drawer::getDefaultSamplerNearest() const {
  return _defaultSamplerNearest;
}

Material * Drawer::getDefaultCheckeredMaterial() const {
  return _defaultCheckeredMaterial;
}

ShaderManager *Drawer::getShaderManager() const {
  return _shaderManager;
}

vk::DescriptorSetLayout Drawer::getSceneDescriptorLayout() const {
  return _sceneDescriptorSetLayout;
}

void Drawer::draw() {

  const auto frame = getCurrentFrame();
  // Wait for gpu to finish past work
  vk::resultCheck(
      _device.waitForFences({frame->getRenderFence()}, true, 1000000000),
      "Wait For Fences Failed");

  frame->cleaner.run();
  frame->getDescriptorAllocator()->clearPools();

  _device.resetFences({frame->getRenderFence()});

  // Request image index from swapchain
  uint32_t swapchainImageIndex;

  try {
    const auto _ = _device.acquireNextImageKHR(_swapchain, 1000000000,
                                               frame->getSwapchainSemaphore(),
                                               nullptr, &swapchainImageIndex);
  } catch (vk::OutOfDateKHRError &_) {
    _resizePending = true;
    return;
  }

  const auto cmd = frame->getCmd();

  // Clear command buffer and prepare to render
  cmd->reset();

  constexpr auto commandBeginInfo = vk::CommandBufferBeginInfo(
      vk::CommandBufferUsageFlagBits::eOneTimeSubmit);

  const auto swapchainExtent = getSwapchainExtent();

  const vk::Extent2D drawExtent = getDrawImageExtent();

  cmd->begin(commandBeginInfo);

  // Transition image to general layout
  transitionImage(*cmd, _drawImage.image,
                  vk::ImageLayout::eUndefined, vk::ImageLayout::eGeneral);

  drawBackground(frame);

  transitionImage(*cmd, _drawImage.image,
                  vk::ImageLayout::eGeneral,
                  vk::ImageLayout::eColorAttachmentOptimal);
  transitionImage(*cmd, _depthImage.image,
                  vk::ImageLayout::eUndefined,
                  vk::ImageLayout::eDepthAttachmentOptimal);

  drawScenes(frame);

  // Transition images to correct transfer layouts
  transitionImage(*cmd, _drawImage.image,
                  vk::ImageLayout::eColorAttachmentOptimal,
                  vk::ImageLayout::eTransferSrcOptimal);
  transitionImage(*cmd, _swapchainImages[swapchainImageIndex],
                  vk::ImageLayout::eUndefined,
                  vk::ImageLayout::eTransferDstOptimal);

  copyImageToImage(*cmd, _drawImage.image,
                   _swapchainImages[swapchainImageIndex],
                   drawExtent, swapchainExtent);

  transitionImage(*cmd, _swapchainImages[swapchainImageIndex],
                  vk::ImageLayout::eTransferDstOptimal,
                  vk::ImageLayout::eColorAttachmentOptimal);

  drawImGui(*cmd, _swapchainImageViews[swapchainImageIndex]);

  transitionImage(*cmd, _swapchainImages[swapchainImageIndex],
                  vk::ImageLayout::eColorAttachmentOptimal,
                  vk::ImageLayout::ePresentSrcKHR
      );

  // Cant add commands anymore
  cmd->end();

  const auto cmdInfo = vk::CommandBufferSubmitInfo(*cmd, 0);

  const auto waitingInfo = vk::SemaphoreSubmitInfo(
      frame->getSwapchainSemaphore(), 1,
      vk::PipelineStageFlagBits2::eColorAttachmentOutput);
  const auto signalInfo = vk::SemaphoreSubmitInfo(
      frame->getRenderSemaphore(), 1, vk::PipelineStageFlagBits2::eAllGraphics);

  const auto submitInfo = vk::SubmitInfo2({}, {waitingInfo}, {cmdInfo},
                                          {signalInfo});

  _graphicsQueue.submit2({submitInfo}, frame->getRenderFence());

  const auto renderSemaphore = frame->getRenderSemaphore();
  const auto presentInfo = vk::PresentInfoKHR({renderSemaphore},
                                              {_swapchain},
                                              swapchainImageIndex);

  try {
    const auto _ = _graphicsQueue.presentKHR(presentInfo);
  } catch (vk::OutOfDateKHRError &_) {
    _resizePending = true;
    return;
  }

  frameCount++;
}
} // namespace rendering
} // namespace vengine
