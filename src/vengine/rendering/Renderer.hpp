#pragma once
#include "vengine/Object.hpp"
#include "vengine/containers/Array.hpp"

#include <optional>
#include <vulkan/vulkan.hpp>

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
namespace rendering {

class Viewport;

/**
 * \brief Base class for the engine renderer. uses vulkan
 */

struct QueueInfo {
  uint32_t familyIndex = 0;
  uint32_t queueCount = 0;
};
class Renderer : public Object {

  long long frameCount = 0;
  
  vk::Instance instance = nullptr;
  
#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
  vk::DebugUtilsMessengerEXT debugMessenger = nullptr;
#endif
  
  vk::PhysicalDevice gpu = nullptr;
  vk::Device device = nullptr;
  vk::SurfaceKHR surface = nullptr;
  vk::SwapchainKHR swapchain = nullptr;
  vk::Format swapchainImageFormat = vk::Format::eUndefined;
  std::vector<vk::Image> swapchainImages;
  std::vector<vk::ImageView> swapchainImageViews;

  vk::Queue graphicsQueue = nullptr;
  uint32_t graphicsQueueFamily = -1;

  vk::CommandPool commandPool = nullptr;
  vk::CommandBuffer mainCommandBuffer = nullptr;

  vk::RenderPass renderPass = nullptr;

  std::vector<vk::Framebuffer> frameBuffers;

  vk::Semaphore presentSemaphore, renderSemaphore;
  vk::Fence renderFence;

  Array<Viewport *> viewports;

  Engine *_engine = nullptr;

  

protected:

  void initSwapchain();

  void initCommands();

  void initDefaultRenderPass();

  void initFrameBuffers();

  void initSyncStructures();
  
public:
  void setEngine(Engine *newEngine);
  Engine *getEngine();
  void init() override;
  void destroy() override;

  void addViewport(Viewport * viewport);

  void removeViewport(const Viewport * viewport);

  virtual void render();
};
} // namespace rendering
} // namespace vengine
