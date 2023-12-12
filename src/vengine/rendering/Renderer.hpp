#pragma once
#include "vengine/Object.hpp"
#include <optional>
#include <vulkan/vulkan.hpp>

namespace vengine {
class Engine;
}

namespace vengine {
namespace world {
class World;
}
} // namespace vengine

namespace vengine {
namespace rendering {

/**
 * \brief Base class for the engine renderer. uses vulkan
 */

struct QueueInfo {
  uint32_t familyIndex = 0;
  uint32_t queueCount = 0;
};
class Renderer : public Object {
  vk::Instance instance = nullptr;
  vk::PhysicalDevice physicalDevice = nullptr;
  vk::Device virtualDevice = nullptr;
  vk::Queue graphicsQueue = nullptr;
  vk::Queue presentationQueue = nullptr;
  vk::SurfaceKHR surface = nullptr;

  std::optional<QueueInfo> graphicsQueueInfo;

  std::optional<QueueInfo> presentationQueueInfo;

  Engine *_engine = nullptr;

protected:
  void createVulkanInstance();
  void pickPhysicalDevice();
  void createQueues();

  void createSurface();

public:
  void setEngine(Engine *newEngine);
  Engine *getEngine();
  void init() override;
  void destroy() override;
};
} // namespace rendering
} // namespace vengine
