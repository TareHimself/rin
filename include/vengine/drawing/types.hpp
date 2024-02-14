#pragma once
#include "Allocator.hpp"
#include "descriptors.hpp"
#include "vengine/types.hpp"
#include "vengine/containers/Array.hpp"
#include "vengine/containers/String.hpp"
#include <glm/fwd.hpp>
#include <glm/glm.hpp>

namespace vengine {
namespace drawing {
class SceneDrawer;
}
}

namespace vengine::drawing {

VENGINE_SIMPLE_BUFFER_SERIALIZER(Buffer,vk::Format);
VENGINE_SIMPLE_BUFFER_SERIALIZER(Buffer,vk::Filter);

enum class EMaterialType : uint8_t {
  MATERIAL_PASS_MAX,
  Lit,
  Unlit,
  Translucent,
  UI,
  Compute
};
struct RawFrameData {
private:
  vk::Semaphore _swapchainSemaphore,_renderSemaphore;
  vk::Fence _renderFence;
  vk::CommandPool _cmdPool;
  vk::CommandBuffer _cmdBuffer;
  DescriptorAllocatorGrowable _frameDescriptors{};
  DrawingSubsystem * _drawer = nullptr;
public:
  CleanupQueue cleaner;
  vk::CommandBuffer * GetCmd();
  vk::CommandPool * GetCmdPool();
  DescriptorAllocatorGrowable * GetDescriptorAllocator();
  vk::Semaphore GetSwapchainSemaphore() const;
  vk::Semaphore GetRenderSemaphore() const;
  vk::Fence GetRenderFence() const;
  DrawingSubsystem * GetDrawer() const;
  void SetSemaphores(const vk::Semaphore &swapchain, const vk::Semaphore &render);
  void SetRenderFence(vk::Fence renderFence);
  void SetCommandPool(vk::CommandPool pool);
  void SetCommandBuffer(vk::CommandBuffer buffer);
  void SetDrawer(DrawingSubsystem * drawer);
};


struct VertexInputDescription {
  Array<vk::VertexInputBindingDescription> bindings;
  Array<vk::VertexInputAttributeDescription> attributes;
}; 

struct Vertex {
  glm::vec4 location;
  glm::vec4 normal;
  glm::vec4 uv;

  // static VertexInputDescription getVertexDescription();
};

VENGINE_SIMPLE_ARRAY_SERIALIZER(Buffer,Vertex);

struct GpuMeshBuffers {
  Managed<AllocatedBuffer> indexBuffer;
  Managed<AllocatedBuffer> vertexBuffer;
  vk::DeviceAddress vertexBufferAddress;
};

struct MeshVertexPushConstant {
  glm::mat4 transformMatrix;
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

enum EMaterialSetType {
  Global = 0,
  Static = 1,
  Dynamic = 2
};

struct BasicShaderResourceInfo {
  EMaterialSetType set = Global;
  uint32_t binding = 0;
  uint32_t count = 1;
  BasicShaderResourceInfo();
  BasicShaderResourceInfo(uint32_t _set,uint32_t _binding,uint32_t _count);
};

// struct TextureInfo {
//   EMaterialSetType set = Global;
//   uint32_t binding = 0;
//   bool bIsArray = false;
//   TextureInfo();
//   TextureInfo(uint32_t _set,uint32_t _binding,bool _bIsArray);
// };
struct PushConstantInfo {

  uint32_t offset = 0;
  uint32_t size = 0;
  vk::ShaderStageFlags stages{};

  PushConstantInfo();
  PushConstantInfo(uint32_t inOffset, uint32_t _size,vk::ShaderStageFlags _stages);
  
};
struct ShaderResources {
  
  std::unordered_map<std::string,BasicShaderResourceInfo> images;
  std::unordered_map<std::string,PushConstantInfo> pushConstants;
  std::unordered_map<std::string,BasicShaderResourceInfo> uniformBuffers;
};

enum EMaterialResourceType {
  Image,
  PushConstant,
  UniformBuffer
};



struct MaterialResourceInfo {
  EMaterialResourceType type;
  vk::ShaderStageFlagBits stages;
  uint32_t binding;
};

struct SimpleFrameData {
private:
  RawFrameData * _frame = nullptr;
  SceneDrawer * _drawer = nullptr;
public:
  
  SimpleFrameData(RawFrameData * frame);

  vk::CommandBuffer * GetCmd() const;

  CleanupQueue * GetCleaner() const;

  RawFrameData * GetRaw() const;
  SceneDrawer * GetDrawer() const;
  void SetDrawer(SceneDrawer * drawer);

  void DrawQuad() const;
};

}
