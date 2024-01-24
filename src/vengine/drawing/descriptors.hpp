#pragma once
#include <vulkan/vulkan.hpp>
#include "vengine/containers/Array.hpp"
#include <deque>

namespace vengine::drawing {
struct DescriptorLayoutBuilder {
  std::unordered_map<uint32_t,vk::DescriptorSetLayoutBinding> bindings;

  DescriptorLayoutBuilder& AddBinding(uint32_t binding,vk::DescriptorType type,vk::ShaderStageFlags stages);
  DescriptorLayoutBuilder& Clear();
  vk::DescriptorSetLayout Build(vk::Device device);
};

struct DescriptorAllocator {
  
private:
  vk::Device _device;
  vk::DescriptorPool pool;
public:
  struct PoolSizeRatio {
    vk::DescriptorType type;
    float ratio;
  };
  void Init(vk::Device device,uint32_t maxSets, std::span<PoolSizeRatio> poolRatios);
  void Clear() const;
  void Destroy() const;

  vk::DescriptorSet Allocate(vk::DescriptorSetLayout layout) const;
};

struct DescriptorWriter {
private:
  std::deque<vk::DescriptorImageInfo> imageInfos;
  std::deque<vk::DescriptorBufferInfo> bufferInfos;
  Array<vk::WriteDescriptorSet> writes;
public:

  void WriteImage(uint32_t binding,vk::ImageView image,vk::Sampler sampler,vk::ImageLayout layout,vk::DescriptorType type);
  
  void WriteBuffer(uint32_t binding,vk::Buffer buffer,size_t size,size_t offset,vk::DescriptorType type);

  void Clear();
  
  void UpdateSet(vk::Device device,vk::DescriptorSet set);

  DescriptorWriter() {
    Clear();
  }
};

struct DescriptorAllocatorGrowable {

public:
  struct PoolSizeRatio {
    vk::DescriptorType type;
    float ratio;
  };

  void Init(vk::Device device,uint32_t maxSets, std::span<PoolSizeRatio> poolRatios);
  void ClearPools();
  void DestroyPools();

  vk::DescriptorSet Allocate(vk::DescriptorSetLayout layout);
private:

  vk::DescriptorPool GetPool();
  vk::DescriptorPool CreatePool(uint32_t setCount, std::span<PoolSizeRatio> poolRatios) const;
  
  vk::Device _device;
  Array<PoolSizeRatio> _ratios;
  Array<vk::DescriptorPool> _fullPools;
  Array<vk::DescriptorPool> _readyPools;
  uint32_t _setsPerPool;
};
}
