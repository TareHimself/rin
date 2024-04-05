#include <aerox/drawing/types.hpp>

namespace aerox::drawing {

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

DrawingSubsystem * RawFrameData::GetDrawer() const {
  return _drawer;
}

WindowDrawer * RawFrameData::GetWindowDrawer() const {
  return _windowDrawer;
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

void RawFrameData::SetDrawer(DrawingSubsystem *drawer) {
  _drawer = drawer;
}

void RawFrameData::SetWindowDrawer(WindowDrawer *windowDrawer) {
  _windowDrawer = windowDrawer;
}


BasicShaderResourceInfo::BasicShaderResourceInfo() = default;

BasicShaderResourceInfo::BasicShaderResourceInfo(uint32_t _set,
    uint32_t _binding, uint32_t _count) {
  set = static_cast<EMaterialSetType>(_set);
  binding = _binding;
  count = _count;
}

// TextureInfo::TextureInfo() {
// }
//
// TextureInfo::TextureInfo(const uint32_t _set, const uint32_t _binding, const bool _bIsArray) {
//   set = static_cast<EMaterialSetType>(_set);
//   binding = _binding;
//   bIsArray = _bIsArray;
// }

PushConstantInfo::PushConstantInfo() = default;

PushConstantInfo::PushConstantInfo(uint32_t inOffset, uint32_t _size,
    vk::ShaderStageFlags _stages) {
  offset = inOffset;
  size = _size;
  stages = _stages;
  
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

void SimpleFrameData::DrawQuad() const {
  GetCmd()->draw(6,1,0,0);
}

json extent3dToJson(vk::Extent3D ex) {
    return {{"width",0},{"height",0},{"depth",0}};
}

vk::Extent3D extent3dFromJson(json ex) {
  return vk::Extent3D{ex["width"],ex["height"],ex["depth"]};
}

META_DECLARE() {
  using namespace meta;
  
  TypeBuilder()
  META_SIMPLE_STATIC_SERIALIZATION(vk::Filter,META_SERIALIZATION_KEY_ID,static_cast<int>(val),
                                   static_cast<vk::Filter>(static_cast<int>(val)))
  .Create<vk::Filter>("vk::Filter");

  TypeBuilder()
  META_SIMPLE_STATIC_SERIALIZATION(vk::Format,META_SERIALIZATION_KEY_ID,static_cast<int>(val),
                                   static_cast<vk::Format>(static_cast<int>(val)))
  .Create<vk::Format>("vk::Format");
  
  TypeBuilder()
  META_SIMPLE_STATIC_SERIALIZATION(vk::Extent3D,META_SERIALIZATION_KEY_ID,extent3dToJson(val),extent3dFromJson(val))
  .Create<vk::Extent3D>("vk::Extent");
}

}
