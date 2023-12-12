#include "Renderer.hpp"

#include "vengine/Engine.hpp"

#include <set>


namespace vengine {
namespace rendering {


void Renderer::createVulkanInstance() {
  uint32_t version{0};
  vkEnumerateInstanceVersion(&version);

  // Set patch to 0
  version &= ~(0xFFFU);

  constexpr auto engineVersion = vk::makeVersion(1,0,0);
  constexpr auto appVersion = vk::makeVersion(1,0,0);

  const auto appInfo = vk::ApplicationInfo(
      getEngine()->getApplicationName().c_str(), appVersion, "VEngine",engineVersion ,
      version);

  uint32_t glfwExtensionCount = 0;
  const auto glfwExtensions = glfwGetRequiredInstanceExtensions(
      &glfwExtensionCount);

  const std::vector extensions(glfwExtensions,
                               glfwExtensions + glfwExtensionCount);

  const auto createInfo = vk::InstanceCreateInfo(vk::InstanceCreateFlags(), &appInfo,
                                           0, nullptr,
                                           static_cast<uint32_t>(extensions.
                                             size()), extensions.data());

  try {
    instance = vk::createInstance(createInfo, nullptr);
  } catch (vk::SystemError err) {
    throw std::runtime_error(
        "Failed to create vulkan instance: \n" + std::string(err.what()));
  }
}

void Renderer::pickPhysicalDevice() {
  const auto devices = instance.enumeratePhysicalDevices();
  for(const auto vkDevice : devices) {

    if(graphicsQueueInfo.has_value() && presentationQueueInfo.has_value()) {
      break;
    }
    
    //const auto properties = vkDevice.getProperties();
    const auto features = vkDevice.getFeatures();
    const auto queueFamilies = vkDevice.getQueueFamilyProperties();
    
    if(!features.geometryShader) {
      continue;
    }

    
    uint32_t idx = 0;
    
    for(const auto queueFam : queueFamilies) {
      if(graphicsQueueInfo.has_value() && presentationQueueInfo.has_value()) {
        break;
      }

      if(!graphicsQueueInfo.has_value()) {
        if(queueFam.queueFlags && VK_QUEUE_GRAPHICS_BIT) {
          QueueInfo info;
          info.familyIndex = idx;
          info.queueCount = queueFam.queueCount;
          graphicsQueueInfo = info;
        }
      }

      if(!presentationQueueInfo.has_value()) {
        if(vkDevice.getSurfaceSupportKHR(idx,surface)) {
          QueueInfo info;
          info.familyIndex = idx;
          info.queueCount = queueFam.queueCount;
          presentationQueueInfo = info;
        }
      }
      
      idx ++;
        
    }

    if(graphicsQueueInfo.has_value() && presentationQueueInfo.has_value()) {
      physicalDevice = vkDevice;
      break;
    } else {
      graphicsQueueInfo.reset();
      presentationQueueInfo.reset();
    }
  }

  assert(graphicsQueueInfo.has_value() && presentationQueueInfo.has_value() && physicalDevice == nullptr,"Failed to select a graphic device");
}

void Renderer::createQueues() {
  assert(graphicsQueueInfo.has_value() && presentationQueueInfo.has_value(),"Invalid Queue info");
  
  std::vector<vk::DeviceQueueCreateInfo> queueCreateInfos;
  
  const std::set uniqueQueueFamilies = { graphicsQueueInfo->familyIndex, presentationQueueInfo->familyIndex};
  
  for(const auto queueFamily : uniqueQueueFamilies) {
    auto queueCreateInfo = vk::DeviceQueueCreateInfo(vk::DeviceQueueCreateFlags(),queueFamily,static_cast<uint32_t>(1));
    queueCreateInfos.push_back(queueCreateInfo);
  }
  
  const auto deviceCreateInfo = vk::DeviceCreateInfo(vk::DeviceCreateFlags(),queueCreateInfos);
  
  virtualDevice = physicalDevice.createDevice(deviceCreateInfo);
  graphicsQueue = virtualDevice.getQueue(graphicsQueueInfo->familyIndex,0);
  presentationQueue = virtualDevice.getQueue(presentationQueueInfo->familyIndex,0);
}

void Renderer::createSurface() {
  VkSurfaceKHR rawSurf;
  if(glfwCreateWindowSurface(instance,getEngine()->getWindow(),nullptr,&rawSurf) != VK_SUCCESS) {
    throw std::runtime_error(
        "Failed to create surface");
  }
  surface = rawSurf;
}

void Renderer::setEngine(Engine *newEngine) {
  _engine = newEngine;
}

Engine *Renderer::getEngine() {
  return _engine;
}

void Renderer::init() {
  Object::init();
  createVulkanInstance();
  pickPhysicalDevice();
  createQueues();
}

void Renderer::destroy() {
  Object::destroy();
  instance.destroySurfaceKHR(surface,nullptr);
  surface = nullptr;
  virtualDevice.destroy();
  virtualDevice = nullptr;
  graphicsQueue = nullptr;
  physicalDevice = nullptr;
  instance.destroy();
  instance = nullptr;
}

}
}
