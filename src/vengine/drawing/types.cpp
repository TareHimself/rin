#include "types.hpp"


namespace vengine::drawing {
vk::CommandBuffer * RawFrameData::GetCmd() {
  return &_cmdBuffer;
}

vk::CommandPool * RawFrameData::GetCmdPool() {
  return &_cmdPool;
}

DescriptorAllocatorGrowable * RawFrameData::GetDescriptorAllocator() {
  return &_frameDescriptors;
}

vk::Semaphore RawFrameData::GetSwapchainSemaphore() const {
  return _swapchainSemaphore;
}

vk::Semaphore RawFrameData::GetRenderSemaphore() const {
  return _renderSemaphore;
}

vk::Fence RawFrameData::GetRenderFence() const {
  return _renderFence;
}

void RawFrameData::SetSemaphores(const vk::Semaphore &swapchain, const vk::Semaphore &render) {
  _swapchainSemaphore = swapchain;
  _renderSemaphore = render;
}

void RawFrameData::SetRenderFence(const vk::Fence renderFence) {
  _renderFence = renderFence;
}

void RawFrameData::SetCommandPool(const vk::CommandPool pool) {
  _cmdPool = pool;
}

void RawFrameData::SetCommandBuffer(const vk::CommandBuffer buffer) {
  _cmdBuffer = buffer;
}



BasicShaderResourceInfo::BasicShaderResourceInfo() = default;

BasicShaderResourceInfo::BasicShaderResourceInfo(const uint32_t _set,
                                                 const uint32_t _binding) {
  set = _set;
  binding = _binding;
}

PushConstantInfo::PushConstantInfo() = default;

PushConstantInfo::PushConstantInfo(const uint32_t _size, const vk::ShaderStageFlags _stages) {
  size = _size;
  stages = _stages;
  offset = 0;
}


SimpleFrameData::SimpleFrameData(RawFrameData *frame) {
  _frame = frame;
}

vk::CommandBuffer * SimpleFrameData::GetCmd() const {
  return _frame->GetCmd();
}


CleanupQueue * SimpleFrameData::GetCleaner() const {
  return &_frame->cleaner;
}

RawFrameData * SimpleFrameData::GetRaw() const {
  return _frame;
}

}
