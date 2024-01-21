#include "types.hpp"


namespace vengine::drawing {
vk::CommandBuffer * FrameData::GetCmd() {
  return &_cmdBuffer;
}

vk::CommandPool * FrameData::GetCmdPool() {
  return &_cmdPool;
}

DescriptorAllocatorGrowable * FrameData::GetDescriptorAllocator() {
  return &_frameDescriptors;
}

vk::Semaphore FrameData::GetSwapchainSemaphore() const {
  return _swapchainSemaphore;
}

vk::Semaphore FrameData::GetRenderSemaphore() const {
  return _renderSemaphore;
}

vk::Fence FrameData::GetRenderFence() const {
  return _renderFence;
}

void FrameData::SetSemaphores(const vk::Semaphore &swapchain, const vk::Semaphore &render) {
  _swapchainSemaphore = swapchain;
  _renderSemaphore = render;
}

void FrameData::SetRenderFence(const vk::Fence renderFence) {
  _renderFence = renderFence;
}

void FrameData::SetCommandPool(const vk::CommandPool pool) {
  _cmdPool = pool;
}

void FrameData::SetCommandBuffer(const vk::CommandBuffer buffer) {
  _cmdBuffer = buffer;
}

}
