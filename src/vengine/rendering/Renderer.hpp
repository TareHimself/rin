#pragma once
#include "ShaderCompiler.hpp"
#include "vengine/Object.hpp"
#include "vengine/containers/Array.hpp"
#include <filesystem>

#include <glslang/Public/ShaderLang.h>
#include <glslang/SPIRV/GlslangToSpv.h>
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
class Renderer : public Object<Engine> {

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
  Array<vk::Image> swapchainImages;
  Array<vk::ImageView> swapchainImageViews;

  vk::Queue graphicsQueue = nullptr;
  uint32_t graphicsQueueFamily = -1;

  vk::CommandPool commandPool = nullptr;
  vk::CommandBuffer mainCommandBuffer = nullptr;

  vk::RenderPass renderPass = nullptr;

  Array<vk::Framebuffer> frameBuffers;

  Array<vk::ShaderModule> shaders;

  vk::Semaphore presentSemaphore, renderSemaphore;
  vk::Fence renderFence;

  vk::PipelineLayout pipelineLayout;

  vk::Pipeline pipeline;

protected:

  void initSwapchain();

  void initCommands();

  void initDefaultRenderPass();

  void initFrameBuffers();

  void initSyncStructures();

  void initPipelineLayout();

  void initPipelines();

  
  
public:
  
  vk::ShaderModule loadShaderFromPath(const std::filesystem::path &shaderName);
  
  ShaderCompiler * shaderCompiler = nullptr;
  Engine *getEngine() const;

  void init(Engine *outer) override;
  void onCleanup() override;

  virtual void render();
};
} // namespace rendering
} // namespace vengine
