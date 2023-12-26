#include "Renderer.hpp"

#include "PipelineBuilder.hpp"
#include "vengine/Engine.hpp"
#include "vengine/io/io.hpp"
#include <VkBootstrap.h>
#include <SDL2/SDL.h>
#include <SDL2/SDL_vulkan.h>

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
namespace rendering {
void Renderer::initSwapchain() {
  vkb::SwapchainBuilder swapchainBuilder{gpu, device, surface};

  const auto extent = getEngine()->getWindowExtent();

  vkb::Swapchain vkbSwapchain =
      swapchainBuilder.use_default_format_selection()
                      .set_desired_present_mode(VK_PRESENT_MODE_FIFO_KHR)
                      .set_desired_extent(extent.width, extent.height)
                      .build()
                      .value();

  swapchain = vkbSwapchain.swapchain;

  auto images = vkbSwapchain.get_images().value();

  for (auto &image : images) {
    swapchainImages.emplace_back(image);
  }

  auto imageViews = vkbSwapchain.get_image_views().value();
  for (auto &imageView : imageViews) {
    swapchainImageViews.emplace_back(imageView);
  }

  swapchainImageFormat = static_cast<vk::Format>(vkbSwapchain.image_format);

  addCleanup([this] {
    for (auto i = 0; i < swapchainImages.size(); i++) {
      device.destroyImageView(swapchainImageViews[i]);
    }

    device.destroySwapchainKHR(swapchain);
  });
}

void Renderer::initCommands() {
  const auto commandPoolInfo = vk::CommandPoolCreateInfo(
      vk::CommandPoolCreateFlags(
          vk::CommandPoolCreateFlagBits::eResetCommandBuffer),
      graphicsQueueFamily);

  commandPool = device.createCommandPool(commandPoolInfo, nullptr);

  const auto commandBufferAllocateInfo = vk::CommandBufferAllocateInfo(
      commandPool, vk::CommandBufferLevel::ePrimary, 1);

  mainCommandBuffer = device.allocateCommandBuffers(commandBufferAllocateInfo).
                             at(0);

  addCleanup([this] {
    device.destroyCommandPool(commandPool);
  });
}

void Renderer::initDefaultRenderPass() {
  auto colorAttachment = vk::AttachmentDescription(
      vk::AttachmentDescriptionFlags(), swapchainImageFormat,
      vk::SampleCountFlagBits::e1,
      vk::AttachmentLoadOp::eClear,
      vk::AttachmentStoreOp::eStore,
      vk::AttachmentLoadOp::eDontCare,
      vk::AttachmentStoreOp::eDontCare,
      vk::ImageLayout::eUndefined,
      vk::ImageLayout::ePresentSrcKHR);

  auto colorAttachmentRef = vk::AttachmentReference(
      0, vk::ImageLayout::eColorAttachmentOptimal);

  auto subpass = vk::SubpassDescription(vk::SubpassDescriptionFlags(),
                                        vk::PipelineBindPoint::eGraphics);

  subpass.setColorAttachmentCount(1);
  subpass.setColorAttachments({colorAttachmentRef});

  const auto renderPassCreateInfo = vk::RenderPassCreateInfo(
      vk::RenderPassCreateFlags(), {colorAttachment}, {subpass});

  renderPass = device.createRenderPass(renderPassCreateInfo);

  addCleanup([this] {
    device.destroyRenderPass(renderPass);
  });
}

void Renderer::initFrameBuffers() {

  const auto extent = getEngine()->getWindowExtent();
  auto frameBufferInfo = vk::FramebufferCreateInfo(
      vk::FramebufferCreateFlags(), renderPass, {}, extent.width, extent.height,
      1);

  const auto numImages = swapchainImages.size();
  frameBuffers = Array<vk::Framebuffer>(numImages);

  for (auto i = 0; i < numImages; i++) {
    frameBufferInfo.setAttachments(swapchainImageViews[i]);
    frameBuffers[i] = device.createFramebuffer(frameBufferInfo);
  }

  addCleanup([this] {
    for (auto i = 0; i < frameBuffers.size(); i++) {
      device.destroyFramebuffer(frameBuffers[i]);
    }
  });
}

void Renderer::initSyncStructures() {
  constexpr auto fenceCreateInfo = vk::FenceCreateInfo(
      vk::FenceCreateFlagBits::eSignaled);

  renderFence = device.createFence(fenceCreateInfo);

  constexpr auto semaphoreCreateInfo = vk::SemaphoreCreateInfo(
      vk::SemaphoreCreateFlags());

  presentSemaphore = device.createSemaphore(semaphoreCreateInfo);
  renderSemaphore = device.createSemaphore(semaphoreCreateInfo);
  addCleanup([this] {
    device.destroySemaphore(renderSemaphore);
    device.destroySemaphore(presentSemaphore);
    device.destroyFence(renderFence);
  });
}

void Renderer::initPipelineLayout() {
  pipelineLayout = device.createPipelineLayout(
      vk::PipelineLayoutCreateInfo({}, {}, {}));
  addCleanup([this] {
    device.destroyPipelineLayout(pipelineLayout);
  });
}

void Renderer::initPipelines() {
  PipelineBuilder builder;

  const auto triangleFragShader = loadShaderFromPath(
      io::getRawShaderPath("triangle.frag"));
  const auto triangleVertShader = loadShaderFromPath(
      io::getRawShaderPath("triangle.vert"));

  auto windowExtent = getEngine()->getWindowExtent();
  pipeline = builder
             .vertexInput()
             .inputAssembly(vk::PrimitiveTopology::eTriangleList)
             .addVertexShader(triangleVertShader)

             .rasterizer(vk::PolygonMode::eFill)
             .addFragmentShader(triangleFragShader)
             .addViewport({{0, 0}, windowExtent})
             .addScissor({{0, 0}, windowExtent})

             .multisampling()
             .colorBlendAttachment()
             .setLayout(pipelineLayout)
             .build(device, renderPass);

  addCleanup([this] {
    device.destroyPipeline(pipeline);
  });
}

vk::ShaderModule Renderer::loadShaderFromPath(const std::filesystem::path &shaderPath) {
  const auto compiledShaderData = shaderCompiler->loadShader(shaderPath);

  const auto shaderCreateInfo = vk::ShaderModuleCreateInfo(
      vk::ShaderModuleCreateFlags(),
      compiledShaderData.size() * sizeof(uint32_t), compiledShaderData.data());

  auto shaderModule = device.createShaderModule(shaderCreateInfo);

  shaders.push(shaderModule);
  
  return shaderModule;
}

Engine *Renderer::getEngine() const { return dynamic_cast<Engine *>(getOuter()); }

void Renderer::init(Engine *outer) {
  Object<Engine>::init(outer);
  vkb::InstanceBuilder builder;

  auto instanceResult =
      builder.set_app_name(getEngine()->getApplicationName().c_str())
             .require_api_version(1, 1, 0)
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

  SDL_Vulkan_CreateSurface(window, instance, &tempSurf);

  surface = tempSurf;

  vkb::PhysicalDeviceSelector selector{vkbInstance};
  vkb::PhysicalDevice physicalDevice =
      selector.set_minimum_version(1, 1).set_surface(surface).select().value();

  vkb::DeviceBuilder deviceBuilder{physicalDevice};

  vkb::Device vkbDevice = deviceBuilder.build().value();

  device = vkbDevice.device;

  gpu = physicalDevice.physical_device;

  graphicsQueue = vkbDevice.get_queue(vkb::QueueType::graphics).value();

  graphicsQueueFamily = vkbDevice.get_queue_index(vkb::QueueType::graphics).
                                  value();

  addCleanup([this] {
    for(const auto shader : shaders) {
      device.destroyShaderModule(shader);
    }

    shaders.clear();
    
    device.destroy();
    instance.destroySurfaceKHR(surface);

#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
instance.destroyDebugUtilsMessengerEXT(debugMessenger);
#endif

    instance.destroy();
  });

  initSwapchain();

  initCommands();

  initDefaultRenderPass();

  initFrameBuffers();

  initSyncStructures();

  shaderCompiler = newObject<ShaderCompiler>();
  shaderCompiler->init(this);

  initPipelineLayout();

  initPipelines();
}


void Renderer::onCleanup() {
  // Wait for the device to idle
  device.waitIdle();

  Object::onCleanup();

  shaderCompiler->cleanup();
}

void Renderer::render() {

  // Wait for gpu to finish past work
  vk::resultCheck(device.waitForFences({renderFence}, true, 1000000000),
                  "Wait For Fences Failed");
  device.resetFences({renderFence});
  //vk::resultCheck(device.resetFences(renderFence,true,1000000000),"Reset Fences Failed");

  // Request image index from swapchain
  uint32_t swapchainImageIndex;
  vk::resultCheck(
      device.acquireNextImageKHR(swapchain, 1000000000, presentSemaphore,
                                 nullptr, &swapchainImageIndex),
      "Acquire Next Image Failed");

  // Clear command buffer and prepare to render
  mainCommandBuffer.reset();

  const auto cmd = mainCommandBuffer;

  constexpr auto commandBeginInfo = vk::CommandBufferBeginInfo(
      vk::CommandBufferUsageFlagBits::eOneTimeSubmit);

  cmd.begin(commandBeginInfo);

  float flash = abs(sin(frameCount / 120.f));

  auto clearValue = vk::ClearValue({0.0f, 0.0f, 0.0f, 0.0f});

  const auto extent = getEngine()->getWindowExtent();
  const auto renderPassInfo = vk::RenderPassBeginInfo(
      renderPass, frameBuffers[swapchainImageIndex],
      {{0, 0}, extent}, clearValue);
  // Begin pass
  cmd.beginRenderPass(renderPassInfo, vk::SubpassContents::eInline);

  // //Actual Rendering
  // for (const auto scene : getEngine()->getScenes()) {
  //   scene->render(this,&cmd);
  // }

  cmd.bindPipeline(vk::PipelineBindPoint::eGraphics, pipeline);

  cmd.draw(3, 1, 0, 0);

  // Finalize Render Pass
  cmd.endRenderPass();

  // Cant add commands anymore
  cmd.end();

  // Submit to queue
  auto waitDstStageMask = vk::PipelineStageFlags(
      vk::PipelineStageFlagBits::eColorAttachmentOutput);

  const auto submitInfo = vk::SubmitInfo({presentSemaphore}, {waitDstStageMask},
                                         {cmd}, {renderSemaphore});

  graphicsQueue.submit({submitInfo}, {renderFence});

  auto swapchainIndices = {swapchainImageIndex};

  const auto presentInfo = vk::PresentInfoKHR({renderSemaphore}, {swapchain},
                                              swapchainIndices);

  vk::resultCheck(graphicsQueue.presentKHR(presentInfo), "Failed to Present");

  frameCount++;
}
} // namespace rendering
} // namespace vengine
