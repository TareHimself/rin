#include "descriptors.hpp"

#include "vengine/utils.hpp"

#include <ranges>

namespace vengine::drawing {
DescriptorLayoutBuilder & DescriptorLayoutBuilder::AddBinding(const uint32_t binding,
                                                              const vk::DescriptorType type,const vk::ShaderStageFlags stages) {
  if(bindings.contains(binding)) {
    utils::vassert(bindings[binding].descriptorType == type,"HOW HAVE YOU DONE THIS!!!");
    auto existing = bindings[binding];
    existing.setStageFlags(existing.stageFlags | stages);
    bindings.emplace(binding,existing);
    return *this;
  }
  
  auto newBinding = vk::DescriptorSetLayoutBinding(binding,type,1,stages);
  bindings.emplace(binding,newBinding);
  return *this;
}

DescriptorLayoutBuilder & DescriptorLayoutBuilder::Clear() {
  bindings.clear();
  return *this;
}

vk::DescriptorSetLayout DescriptorLayoutBuilder::Build(const vk::Device device) {
  std::vector<vk::DescriptorSetLayoutBinding> bindingsArr;
  
  for(auto val : bindings | std::views::values) {
    bindingsArr.push_back(val);
  }
  
  const auto info = vk::DescriptorSetLayoutCreateInfo({},bindingsArr);

  return device.createDescriptorSetLayout(info);
}

void DescriptorAllocator::Init(const vk::Device device, const uint32_t maxSets,
                               const std::span<PoolSizeRatio> poolRatios) {
  _device = device;
  Array<vk::DescriptorPoolSize> poolSizes;
  for(const auto ratio: poolRatios) {
    vk::DescriptorPoolSize poolSize{ratio.type,static_cast<uint32_t>(ratio.ratio * maxSets)};
    poolSizes.Push(poolSize);
  }

  const vk::DescriptorPoolCreateInfo poolInfo{{},maxSets,poolSizes};

  pool = device.createDescriptorPool(poolInfo);
}

void DescriptorAllocator::Clear() const {
  _device.resetDescriptorPool(pool);
}

void DescriptorAllocator::Destroy() const {
  Clear();
  _device.destroyDescriptorPool(pool);
}

vk::DescriptorSet DescriptorAllocator::Allocate(
    vk::DescriptorSetLayout layout) const {
  const vk::DescriptorSetAllocateInfo allocInfo{pool,{layout}};
  
  return _device.allocateDescriptorSets(allocInfo).at(0);
}

void DescriptorWriter::WriteImage(const uint32_t binding, vk::ImageView image,
                                  vk::Sampler sampler, vk::ImageLayout layout, const vk::DescriptorType type) {
  vk::DescriptorImageInfo& info = imageInfos.emplace_back(sampler,image,layout);

  auto write = vk::WriteDescriptorSet{};
  write.setDstBinding(binding);
  write.setDstSet(nullptr);
  write.setDescriptorType(type);
  //write.descriptorCount = 1;
  write.setImageInfo(info);

  writes.Push(write);
}

void DescriptorWriter::WriteBuffer(const uint32_t binding, vk::Buffer buffer, size_t size,
                                   size_t offset, const vk::DescriptorType type) {
  vk::DescriptorBufferInfo& info = bufferInfos.emplace_back(buffer,offset,size);

  auto write = vk::WriteDescriptorSet{};
  write.setDstBinding(binding);
  write.setDstSet(nullptr);
  write.setDescriptorType(type);
  write.setBufferInfo(info);

  writes.Push(write);
}

void DescriptorWriter::Clear() {
  imageInfos.clear();
  writes.clear();
  bufferInfos.clear();
}

void DescriptorWriter::UpdateSet(const vk::Device device, const vk::DescriptorSet set) {
  for(auto &write : writes) {
    write.setDstSet(set);
  }

  device.updateDescriptorSets(writes,{});
}

void DescriptorAllocatorGrowable::Init(const vk::Device device, const uint32_t maxSets,
                                       const std::span<PoolSizeRatio> poolRatios) {
  _device = device;
  _ratios.clear();
  for(auto r : poolRatios) {
    _ratios.Push(r);
  }

  auto newPool = CreatePool(maxSets,poolRatios);

  _setsPerPool = maxSets * 1.5; // Grow it next alloc

  _readyPools.Push(newPool);
}

void DescriptorAllocatorGrowable::ClearPools() {
  for(auto p : _readyPools) {
    _device.resetDescriptorPool(p);
  }

  for(auto p : _fullPools) {
    _device.resetDescriptorPool(p);
    _readyPools.Push(p);
  }

  _fullPools.clear();
}

void DescriptorAllocatorGrowable::DestroyPools() {
  for(const auto p : _readyPools) {
    _device.destroyDescriptorPool(p);
  }
  _readyPools.clear();
  for(const auto p : _fullPools) {
    _device.destroyDescriptorPool(p);
  }
  _fullPools.clear();
}

vk::DescriptorSet DescriptorAllocatorGrowable::Allocate(
    vk::DescriptorSetLayout layout) {

  // Get or create a pool
  auto poolToUse = GetPool();

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
    _fullPools.Push(poolToUse);
    poolToUse = GetPool();
    allocInfo.setDescriptorPool(poolToUse);

    descriptorSet = _device.allocateDescriptorSets(allocInfo).at(0);
  }
  
  _readyPools.Push(poolToUse);
  
  return descriptorSet;
}

vk::DescriptorPool DescriptorAllocatorGrowable::GetPool() {
  vk::DescriptorPool newPool;
  if(!_readyPools.empty()) {
    newPool = _readyPools.back();
    _readyPools.Pop();
  } else {
    newPool = CreatePool(_setsPerPool,_ratios);

    _setsPerPool = _setsPerPool * 1.5;
    if(_setsPerPool > 4092) {
      _setsPerPool = 4092;
    }
  }

  return newPool;
}

vk::DescriptorPool DescriptorAllocatorGrowable::CreatePool(const uint32_t setCount,
                                                           const std::span<PoolSizeRatio> poolRatios) const {
  Array<vk::DescriptorPoolSize> poolSizes;
  for(const auto ratio : poolRatios) {
    auto poolSize = vk::DescriptorPoolSize{ratio.type,static_cast<unsigned>(ratio.ratio * setCount)};
    poolSizes.Push(poolSize);
  }

  const auto poolInfo = vk::DescriptorPoolCreateInfo{{},setCount,poolSizes};
  
  return  _device.createDescriptorPool(poolInfo);
}
}
