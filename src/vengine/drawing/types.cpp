#include "types.hpp"


namespace vengine {
namespace drawing {
vk::CommandBuffer * FrameData::getCmd() {
  return &_cmdBuffer;
}

vk::CommandPool * FrameData::getCmdPool() {
  return &_cmdPool;
}

DescriptorAllocatorGrowable * FrameData::getDescriptorAllocator() {
  return &_frameDescriptors;
}

vk::Semaphore FrameData::getSwapchainSemaphore() const {
  return _swapchainSemaphore;
}

vk::Semaphore FrameData::getRenderSemaphore() const {
  return _renderSemaphore;
}

vk::Fence FrameData::getRenderFence() const {
  return _renderFence;
}

void FrameData::setSemaphores(const vk::Semaphore &swapchain, const vk::Semaphore &render) {
  _swapchainSemaphore = swapchain;
  _renderSemaphore = render;
}

void FrameData::setRenderFence(vk::Fence renderFence) {
  _renderFence = renderFence;
}

void FrameData::setCommandPool(vk::CommandPool pool) {
  _cmdPool = pool;
}

void FrameData::setCommandBuffer(vk::CommandBuffer buffer) {
  _cmdBuffer = buffer;
}

VertexInputDescription Vertex::getVertexDescription() {

  VertexInputDescription description;

  // 1 vertex buffer binding
  auto mainBinding = vk::VertexInputBindingDescription(0,sizeof(Vertex),vk::VertexInputRate::eVertex);

  description.bindings.push(mainBinding);

  // Location [Location 0]
  auto locationAttribute = vk::VertexInputAttributeDescription(0,0,vk::Format::eR32G32B32Sfloat,offsetof(Vertex,location));

  // Normal [Location 1]
  auto normalAttribute = vk::VertexInputAttributeDescription(1,0,vk::Format::eR32G32B32Sfloat,offsetof(Vertex,normal));
  
  // Color [Location 2]
  auto colorAttribute = vk::VertexInputAttributeDescription(2,0,vk::Format::eR32G32B32Sfloat,offsetof(Vertex,color));

  description.attributes.push(locationAttribute);
  description.attributes.push(normalAttribute);
  description.attributes.push(colorAttribute);

  return description;
}
}
}