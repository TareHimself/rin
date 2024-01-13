#pragma once
#include "descriptors.hpp"
#include "vengine/types.hpp"
#include "vengine/containers/Array.hpp"
#include "vengine/math/Vector3.hpp"
#include <vk_mem_alloc.hpp>

namespace vengine {
namespace drawing {
enum class EMaterialPass : uint8_t {
  Opaque,
  Translucent,
  Transparent,
  MATERIAL_PASS_MAX
};
struct FrameData {
private:
  vk::Semaphore _swapchainSemaphore,_renderSemaphore;
  vk::Fence _renderFence;
  vk::CommandPool _cmdPool;
  vk::CommandBuffer _cmdBuffer;
  DescriptorAllocatorGrowable _frameDescriptors{};
public:
  CleanupQueue cleaner;
  vk::CommandBuffer * getCmd();
  vk::CommandPool * getCmdPool();
  DescriptorAllocatorGrowable * getDescriptorAllocator();
  vk::Semaphore getSwapchainSemaphore() const;
  vk::Semaphore getRenderSemaphore() const;
  vk::Fence getRenderFence() const;
  void setSemaphores(const vk::Semaphore &swapchain, const vk::Semaphore &render);
  void setRenderFence(vk::Fence renderFence);
  void setCommandPool(vk::CommandPool pool);
  void setCommandBuffer(vk::CommandBuffer buffer);
  
};

struct VmaAllocated {
  vma::Allocation alloc;
};

struct AllocatedBuffer : VmaAllocated {
  vk::Buffer buffer = nullptr;
  vma::AllocationInfo info;
};

struct AllocatedImage :  VmaAllocated{
  vk::Image image = nullptr;
  vk::ImageView view = nullptr;
  vk::Extent3D extent;
  vk::Format format;
};

struct VertexInputDescription {
  Array<vk::VertexInputBindingDescription> bindings;
  Array<vk::VertexInputAttributeDescription> attributes;
}; 

struct Vertex {
  glm::vec3 location;
  float uv_x;
  glm::vec3 normal;
  float uv_y;
  glm::vec4 color;

  static VertexInputDescription getVertexDescription();
};

struct GpuMeshBuffers {
  AllocatedBuffer indexBuffer;
  AllocatedBuffer vertexBuffer;
  vk::DeviceAddress vertexBufferAddress;
};

struct GpuDrawPushConstants {
  glm::mat4 worldMatrix;
  vk::DeviceAddress vertexBuffer;
};

struct ComputePushConstants {
  float time;
  glm::vec4 data1;
  glm::vec4 data2;
  glm::vec4 data3;
  glm::vec4 data4;
};

struct ComputeEffect {
  std::string name;
  vk::Pipeline pipeline;
  vk::PipelineLayout layout;
  uint32_t size = sizeof(ComputePushConstants) + 12;
  uint32_t offset = 0;
  ComputePushConstants data;
};

struct GPUSceneData {
  glm::mat4 view;
  glm::mat4 proj;
  glm::mat4 viewproj;
  glm::vec4 ambientColor;
  glm::vec4 sunlightDirection; // w for sun power
  glm::vec4 sunlightColor;
};

}
}
