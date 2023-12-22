#include "Renderer.hpp"

#include "vengine/Engine.hpp"
#include "vengine/utils.hpp"
#include "vengine/scene/Scene.hpp"

#include <VkBootstrap.h>
#include <SDL2/SDL.h>
#include <SDL2/SDL_vulkan.h>

#include <iostream>

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
}

void Renderer::initCommands() {
  const auto commandPoolInfo = vk::CommandPoolCreateInfo(
      vk::CommandPoolCreateFlags(vk::CommandPoolCreateFlagBits::eResetCommandBuffer), graphicsQueueFamily);

  commandPool = device.createCommandPool(commandPoolInfo, nullptr);

  const auto commandBufferAllocateInfo = vk::CommandBufferAllocateInfo(
      commandPool, vk::CommandBufferLevel::ePrimary,1);

  mainCommandBuffer = device.allocateCommandBuffers(commandBufferAllocateInfo).
                             at(0);
}

void Renderer::initDefaultRenderPass() {
  auto colorAttachment = vk::AttachmentDescription(
      vk::AttachmentDescriptionFlags(), swapchainImageFormat,
      vk::SampleCountFlagBits::e1,
      vk::AttachmentLoadOp::eClear,
      vk::AttachmentStoreOp::eDontCare,
      vk::AttachmentLoadOp::eDontCare,
      vk::AttachmentStoreOp::eDontCare,
      vk::ImageLayout::eUndefined,
      vk::ImageLayout::ePresentSrcKHR);

  auto colorAttachmentRef = vk::AttachmentReference(0,vk::ImageLayout::eColorAttachmentOptimal);

  auto subpass = vk::SubpassDescription(vk::SubpassDescriptionFlags(),vk::PipelineBindPoint::eGraphics);

  subpass.setColorAttachmentCount(1);
  subpass.setColorAttachments({colorAttachmentRef});

  const auto renderPassCreateInfo = vk::RenderPassCreateInfo(vk::RenderPassCreateFlags(),{colorAttachment},{subpass});

  renderPass = device.createRenderPass(renderPassCreateInfo);
}

void Renderer::initFrameBuffers() {

  const auto extent = getEngine()->getWindowExtent();
  auto frameBufferInfo = vk::FramebufferCreateInfo(vk::FramebufferCreateFlags(),renderPass,{},extent.width,extent.height,1);

  auto numImages = swapchainImages.size();
  frameBuffers = std::vector<vk::Framebuffer>(numImages);
  
  for(int i = 0; i < numImages; i++) {
    frameBufferInfo.setAttachments(swapchainImageViews[i]);
    frameBuffers[i] = device.createFramebuffer(frameBufferInfo);
  }
}

void Renderer::initSyncStructures() {
  constexpr auto fenceCreateInfo = vk::FenceCreateInfo(vk::FenceCreateFlagBits::eSignaled);
  
  renderFence = device.createFence(fenceCreateInfo);

  constexpr auto semaphoreCreateInfo = vk::SemaphoreCreateInfo(vk::SemaphoreCreateFlags());

  presentSemaphore = device.createSemaphore(semaphoreCreateInfo);
  renderSemaphore = device.createSemaphore(semaphoreCreateInfo);
  
}

void Renderer::setEngine(Engine *newEngine) { _engine = newEngine; }

Engine *Renderer::getEngine() { return _engine; }

void Renderer::init() {
  Object::init();
  vkb::InstanceBuilder builder;

  auto instanceResult =
      builder.set_app_name(getEngine()->getApplicationName().c_str())
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
      selector.set_surface(surface).select().value();

  vkb::DeviceBuilder deviceBuilder{physicalDevice};

  vkb::Device vkbDevice = deviceBuilder.build().value();

  device = vkbDevice.device;

  gpu = physicalDevice.physical_device;

  graphicsQueue = vkbDevice.get_queue(vkb::QueueType::graphics).value();

  graphicsQueueFamily = vkbDevice.get_queue_index(vkb::QueueType::graphics).
                                  value();

  initSwapchain();

  initCommands();

  initDefaultRenderPass();

  initFrameBuffers();

  initSyncStructures();
}

void Renderer::destroy() {
  Object::destroy();

  // Wait for last draw to complete
  vk::resultCheck(device.waitForFences({renderFence},true,1000000000),"Wait For Fences Failed");

  device.destroySemaphore(renderSemaphore);
  
  device.destroySemaphore(presentSemaphore);

  device.destroyFence(renderFence);

  device.destroyRenderPass(renderPass);

  for(int i = 0; i < frameBuffers.size(); i++) {
    device.destroyFramebuffer(frameBuffers[i]);
    device.destroyImageView(swapchainImageViews[i]);
  }

  
  device.destroyCommandPool(commandPool);

  device.destroySwapchainKHR(swapchain);

  device.destroy();

  instance.destroySurfaceKHR(surface);

#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
  instance.destroyDebugUtilsMessengerEXT(debugMessenger);
#endif

  instance.destroy();
}

void Renderer::addViewport(Viewport *viewport) {
  viewports.push_back(viewport);
}

void Renderer::removeViewport(const Viewport * viewport) {

  for(auto i = 0; i < viewports.size(); i++) {
    if(viewports[i] == viewport) {
      viewports.remove(i);
      break;
    }
  }
}

void Renderer::render() {

  // Start Render Pass
  vk::resultCheck(device.waitForFences({renderFence},true,1000000000),"Wait For Fences Failed");

  device.resetFences({renderFence});
  //vk::resultCheck(device.resetFences(renderFence,true,1000000000),"Reset Fences Failed");

  uint32_t swapchainImageIndex;
  vk::resultCheck(device.acquireNextImageKHR(swapchain,1000000000,presentSemaphore,nullptr,&swapchainImageIndex),"Acquire Next Image Failed");
  
  mainCommandBuffer.reset();

  const auto cmd = mainCommandBuffer;

  constexpr auto commandBeginInfo = vk::CommandBufferBeginInfo(vk::CommandBufferUsageFlagBits::eOneTimeSubmit);
  
  cmd.begin(commandBeginInfo);
  
  auto clearValue = vk::ClearValue({0.0f,0.0f,0.0f, 0.0f});
  
  const auto extent = getEngine()->getWindowExtent();
  // std::cout << "BEFORE RENDER PASS INFO " << frameBuffers[swapchainImageIndex] << std::endl;
  const auto renderPassInfo = vk::RenderPassBeginInfo(renderPass,frameBuffers[swapchainImageIndex],{vk::Offset2D {0,0},extent},clearValue);
  // Begin pass
  cmd.beginRenderPass(renderPassInfo,vk::SubpassContents::eInline);
  
  //Actual Rendering
  for(const auto scene : getEngine()->getScenes()) {
    scene->render(&cmd);
  }
  
  // Finalize Render Pass
  cmd.endRenderPass();

  // Cant add commands anymore
  cmd.end();

  // Submit to queue
  auto waitDstStageMask = vk::PipelineStageFlags(vk::PipelineStageFlagBits::eColorAttachmentOutput);

  const auto submitInfo = vk::SubmitInfo({presentSemaphore},{waitDstStageMask},{cmd},{renderSemaphore});

  vk::resultCheck(graphicsQueue.submit(1,&submitInfo,renderFence),"Failed to submit commands");

  auto presentInfo = vk::PresentInfoKHR({renderSemaphore},{swapchain},{swapchainImageIndex});

  vk::resultCheck(graphicsQueue.presentKHR(presentInfo),"Failed to Present");

  frameCount++;
}
} // namespace rendering
} // namespace vengine
