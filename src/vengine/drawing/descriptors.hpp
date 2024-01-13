#pragma once
#include <vulkan/vulkan.hpp>
#include "vengine/containers/Array.hpp"

#include <deque>

namespace vengine {
namespace drawing {
struct DescriptorLayoutBuilder {
  Array<vk::DescriptorSetLayoutBinding> bindings;

  DescriptorLayoutBuilder& addBinding(uint32_t binding,vk::DescriptorType type);
  DescriptorLayoutBuilder& clear();
  vk::DescriptorSetLayout build(vk::Device device, vk::ShaderStageFlags shaderStages);
};

struct DescriptorAllocator {
  struct PoolSizeRatio {
    vk::DescriptorType type;
    float ratio;
  };
  vk::Device _device;
  vk::DescriptorPool pool;

  void init(vk::Device device,uint32_t maxSets, std::span<PoolSizeRatio> poolRatios);
  void clear() const;
  void destroy() const;

  vk::DescriptorSet allocate(vk::DescriptorSetLayout layout) const;
};

struct DescriptorWriter {
  std::deque<vk::DescriptorImageInfo> imageInfos;
  std::deque<vk::DescriptorBufferInfo> bufferInfos;
  Array<vk::WriteDescriptorSet> writes;

  void writeImage(int binding,vk::ImageView image,vk::Sampler sampler,vk::ImageLayout layout,vk::DescriptorType type);
  
  void writeBuffer(int binding,vk::Buffer buffer,size_t size,size_t offset,vk::DescriptorType type);

  void clear();
  
  void updateSet(vk::Device device,vk::DescriptorSet set);

  DescriptorWriter() {
    clear();
  }
};

struct DescriptorAllocatorGrowable {

public:
  struct PoolSizeRatio {
    vk::DescriptorType type;
    float ratio;
  };

  void init(vk::Device device,uint32_t maxSets, std::span<PoolSizeRatio> poolRatios);
  void clearPools();
  void destroyPools();

  vk::DescriptorSet allocate(vk::DescriptorSetLayout layout);
private:

  vk::DescriptorPool getPool();
  vk::DescriptorPool createPool(uint32_t setCount, std::span<PoolSizeRatio> poolRatios);
  
  vk::Device _device;
  Array<PoolSizeRatio> _ratios;
  Array<vk::DescriptorPool> _fullPools;
  Array<vk::DescriptorPool> _readyPools;
  uint32_t _setsPerPool;
};
}
}
