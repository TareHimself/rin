#include "descriptors.hpp"

namespace vengine {
namespace drawing {
DescriptorLayoutBuilder & DescriptorLayoutBuilder::addBinding(uint32_t binding,
    vk::DescriptorType type) {
  auto newBinding = vk::DescriptorSetLayoutBinding(binding,type,1);
  bindings.push(newBinding);
  return *this;
}

DescriptorLayoutBuilder & DescriptorLayoutBuilder::clear() {
  bindings.clear();
  std::vector<int> arr;
  arr = {1,2,3};
  return *this;
}

vk::DescriptorSetLayout DescriptorLayoutBuilder::build(vk::Device device,
    vk::ShaderStageFlags shaderStages) {
  for(auto& binding : bindings) {
    binding.setStageFlags(shaderStages);
  }

  const auto info = vk::DescriptorSetLayoutCreateInfo({},bindings);

  return device.createDescriptorSetLayout(info);
}

void DescriptorAllocator::init(vk::Device device, uint32_t maxSets,
    std::span<PoolSizeRatio> poolRatios) {
  _device = device;
  Array<vk::DescriptorPoolSize> poolSizes;
  for(const auto ratio: poolRatios) {
    vk::DescriptorPoolSize poolSize{ratio.type,static_cast<uint32_t>(ratio.ratio * maxSets)};
    poolSizes.push(poolSize);
  }

  const vk::DescriptorPoolCreateInfo poolInfo{{},maxSets,poolSizes};

  pool = device.createDescriptorPool(poolInfo);
}

void DescriptorAllocator::clear() const {
  _device.resetDescriptorPool(pool);
}

void DescriptorAllocator::destroy() const {
  clear();
  _device.destroyDescriptorPool(pool);
}

vk::DescriptorSet DescriptorAllocator::allocate(
    vk::DescriptorSetLayout layout) const {
  const vk::DescriptorSetAllocateInfo allocInfo{pool,{layout}};
  
  return _device.allocateDescriptorSets(allocInfo).at(0);
}

void DescriptorWriter::writeImage(const int binding, vk::ImageView image,
                                  vk::Sampler sampler, vk::ImageLayout layout, const vk::DescriptorType type) {
  vk::DescriptorImageInfo& info = imageInfos.emplace_back(sampler,image,layout);

  auto write = vk::WriteDescriptorSet{};
  write.setDstBinding(binding);
  write.setDstSet(nullptr);
  write.setDescriptorType(type);
  //write.descriptorCount = 1;
  write.setImageInfo(info);

  writes.push(write);
}

void DescriptorWriter::writeBuffer(const int binding, vk::Buffer buffer, size_t size,
                                   size_t offset, const vk::DescriptorType type) {
  vk::DescriptorBufferInfo& info = bufferInfos.emplace_back(buffer,offset,size);

  auto write = vk::WriteDescriptorSet{};
  write.setDstBinding(binding);
  write.setDstSet(nullptr);
  write.setDescriptorType(type);
  write.setBufferInfo(info);

  writes.push(write);
}

void DescriptorWriter::clear() {
  imageInfos.clear();
  writes.clear();
  bufferInfos.clear();
}

void DescriptorWriter::updateSet(const vk::Device device, const vk::DescriptorSet set) {
  for(auto &write : writes) {
    write.setDstSet(set);
  }

  device.updateDescriptorSets(writes,{});
}

void DescriptorAllocatorGrowable::init(vk::Device device, uint32_t maxSets,
                                       std::span<PoolSizeRatio> poolRatios) {
  _device = device;
  _ratios.clear();
  for(auto r : poolRatios) {
    _ratios.push(r);
  }

  auto newPool = createPool(maxSets,poolRatios);

  _setsPerPool = maxSets * 1.5; // Grow it next alloc

  _readyPools.push(newPool);
}

void DescriptorAllocatorGrowable::clearPools() {
  for(auto p : _readyPools) {
    _device.resetDescriptorPool(p);
  }

  for(auto p : _fullPools) {
    _device.resetDescriptorPool(p);
    _readyPools.push(p);
  }

  _fullPools.clear();
}

void DescriptorAllocatorGrowable::destroyPools() {
  for(const auto p : _readyPools) {
    _device.destroyDescriptorPool(p);
  }
  _readyPools.clear();
  for(const auto p : _fullPools) {
    _device.destroyDescriptorPool(p);
  }
  _fullPools.clear();
}

vk::DescriptorSet DescriptorAllocatorGrowable::allocate(
    vk::DescriptorSetLayout layout) {

  // Get or create a pool
  auto poolToUse = getPool();

  auto allocInfo = vk::DescriptorSetAllocateInfo{poolToUse,layout};

  vk::DescriptorSet descriptorSet;

  bool failed = true;
  try {
   descriptorSet = _device.allocateDescriptorSets(allocInfo).at(0);
    failed = false;
  } catch (vk::OutOfPoolMemoryError &_) {
    
  }
  catch (vk::FragmentedPoolError &_) {
    
  }

  if(failed) {
    _fullPools.push(poolToUse);
    poolToUse = getPool();
    allocInfo.setDescriptorPool(poolToUse);

    descriptorSet = _device.allocateDescriptorSets(allocInfo).at(0);
  }
  
  _readyPools.push(poolToUse);
  
  return descriptorSet;
}

vk::DescriptorPool DescriptorAllocatorGrowable::getPool() {
  vk::DescriptorPool newPool;
  if(!_readyPools.empty()) {
    newPool = _readyPools.back();
    _readyPools.pop();
  } else {
    newPool = createPool(_setsPerPool,_ratios);

    _setsPerPool = _setsPerPool * 1.5;
    if(_setsPerPool > 4092) {
      _setsPerPool = 4092;
    }
  }

  return newPool;
}

vk::DescriptorPool DescriptorAllocatorGrowable::createPool(uint32_t setCount,
    std::span<PoolSizeRatio> poolRatios) {
  Array<vk::DescriptorPoolSize> poolSizes;
  for(const auto ratio : poolRatios) {
    auto poolSize = vk::DescriptorPoolSize{ratio.type,static_cast<unsigned>(ratio.ratio * setCount)};
    poolSizes.push(poolSize);
  }

  const auto poolInfo = vk::DescriptorPoolCreateInfo{{},setCount,poolSizes};
  
  return  _device.createDescriptorPool(poolInfo);
}
}
}