#pragma once
#include "vengine/Object.hpp"
#include <vulkan/vulkan.hpp>
#include "vengine/containers/Array.hpp"
#include "generated/drawing/descriptors.reflect.hpp"

#include <variant>

namespace vengine {
namespace drawing {
struct AllocatedImage;
}
}

namespace vengine::drawing {
struct AllocatedBuffer;
class DrawingSubsystem;
class Texture2D;


typedef std::variant<std::monostate,Managed<AllocatedBuffer>,Managed<AllocatedImage>,Managed<Texture2D>,Array<Managed<Texture2D>>> descriptorResource;
RCLASS()
class DescriptorSet {
  vk::Device _device;
  vk::DescriptorSet _set;
  std::mutex _mutex;
  std::unordered_map<uint32_t,descriptorResource> _resources;

public:
  DescriptorSet(const vk::Device &device,const vk::DescriptorSet &set);
  ~DescriptorSet();
  operator vk::DescriptorSet() const;

  void WriteBuffer(uint32_t binding, const Ref<AllocatedBuffer> &buffer, size_t
                   offset, vk::DescriptorType type);

  void WriteImage(uint32_t binding, const Ref<AllocatedImage> &image, vk::Sampler
                  sampler, vk::ImageLayout layout, vk::DescriptorType type);
  
  void WriteTexture(uint32_t binding, const Ref<Texture2D> &texture, vk::ImageLayout layout, const
                    vk::DescriptorType type);

  void WriteTextureArray(uint32_t binding, const Array<Ref<Texture2D>> &textures, vk::ImageLayout
                         layout, const vk::DescriptorType type);

  bool IsBound(uint32_t binding) const;
};


REFLECT_IMPLEMENT(DescriptorSet)

struct DescriptorLayoutBuilder {
  std::unordered_map<uint32_t,vk::DescriptorSetLayoutBinding> bindings;

  DescriptorLayoutBuilder& AddBinding(uint32_t binding,vk::DescriptorType type,vk::ShaderStageFlags stages,uint32_t count = 1);
  DescriptorLayoutBuilder& Clear();
  vk::DescriptorSetLayout Build();
};




// struct DescriptorWriter {
// private:
//   Array<std::tuple<uint32_t,Array<vk::DescriptorImageInfo>,vk::DescriptorType>> _imageInfos;
//   Array<std::tuple<uint32_t,Array<vk::DescriptorBufferInfo>,vk::DescriptorType>> _bufferInfos;
// public:
//
//   void WriteImage(uint32_t binding,vk::ImageView image,vk::Sampler sampler,vk::ImageLayout layout,vk::DescriptorType type);
//
//   void WriteTexture(const uint32_t binding, const std::weak_ptr<Texture> &texture, vk::ImageLayout
//                     layout, const vk::DescriptorType type);
//   
//   void WriteTextures(const uint32_t binding, const Array<std::weak_ptr<Texture>> &textures, vk::
//                      ImageLayout layout, const vk::DescriptorType type);
//   
//   void WriteBuffer(uint32_t binding,vk::Buffer buffer,size_t size,size_t offset,vk::DescriptorType type);
//
//   void Clear();
//   
//   void UpdateSet(const vk::Device device, const std::weak_ptr<vk::DescriptorSet> &set);
//
//   DescriptorWriter() {
//     Clear();
//   }
// };

typedef std::pair<uint64_t,vk::DescriptorPool> DescriptorPoolWithId;

struct DescriptorAllocatorGrowable {

public:
  struct PoolSizeRatio {
    vk::DescriptorType type;
    float ratio;
  };

  void Init(vk::Device device,uint32_t maxSets, std::span<PoolSizeRatio> poolRatios);
  void ClearPools();
  void DestroyPools();
  Ref<DescriptorSet> Allocate(vk::DescriptorSetLayout layout);
private:
  std::atomic<uint64_t> _lastPoolId = 0;
  
  void ClearDescriptorsFromPool(DescriptorPoolWithId pool);
  DescriptorPoolWithId GetPool();
  DescriptorPoolWithId CreatePool(uint32_t setCount, std::span<PoolSizeRatio> poolRatios);
  Managed<DescriptorSet> DescriptorSetToPtr(const vk::DescriptorSet &set) const;
  vk::Device _device;
  Array<PoolSizeRatio> _ratios;
  Array<DescriptorPoolWithId> _fullPools;
  Array<DescriptorPoolWithId> _readyPools;
  uint32_t _setsPerPool;
  
  std::unordered_map<uint64_t,Array<Managed<DescriptorSet>>> _descriptorsInUse;
};
}
