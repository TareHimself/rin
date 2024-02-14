#include <vengine/drawing/descriptors.hpp>
#include <vengine/drawing/Texture2D.hpp>
#include <vengine/utils.hpp>
#include <ranges>

namespace vengine::drawing {

DescriptorSet::DescriptorSet(const vk::Device &device,
    const vk::DescriptorSet &set) {
  _device = device;
  _set = set;
}

DescriptorSet::~DescriptorSet() {
  _buffers.clear();
  _textureArrays.clear();
  _images.clear();
  _textures.clear();
}

DescriptorSet::operator vk::DescriptorSet() const {
  return _set;
}


void DescriptorSet::WriteBuffer(uint32_t binding,
                                const Ref<AllocatedBuffer> &buffer, size_t size, size_t offset,
                                vk::DescriptorType type) {
  std::lock_guard guard(_mutex);
  
  const auto shared = buffer.Reserve();
  vk::DescriptorBufferInfo info{shared->buffer,offset,size};
  vk::WriteDescriptorSet write{};
  write.setDstSet(_set);
  write.setDescriptorType(type);
  write.setDstBinding(binding);
  write.setBufferInfo(info);

  _buffers[binding] = shared;
  
  _device.updateDescriptorSets(write,{});
}

void DescriptorSet::WriteImage(uint32_t binding,
    const Ref<AllocatedImage> &image,vk::Sampler sampler, vk::ImageLayout layout,
    vk::DescriptorType type) {
  std::lock_guard guard(_mutex);
  
  const auto shared = image.Reserve();
  vk::DescriptorImageInfo info{sampler,shared->view,layout};
  vk::WriteDescriptorSet write{};
  write.setDstSet(_set);
  write.setDescriptorType(type);
  write.setDstBinding(binding);
  write.setImageInfo(info);

  _images[binding] = shared;
  
  _device.updateDescriptorSets(write,{});
}

void DescriptorSet::WriteTexture(uint32_t binding,
    const Ref<Texture2D> &texture, vk::ImageLayout layout,
    const vk::DescriptorType type) {
  std::lock_guard guard(_mutex);
  
  const auto shared = texture.Reserve();

  if(!shared->IsUploaded()) {
    shared->Upload();
  }
  
  vk::DescriptorImageInfo info{shared->GetSampler(),shared->GetGpuData().Reserve()->view,layout};
  vk::WriteDescriptorSet write{};
  write.setDstSet(_set);
  write.setDescriptorType(type);
  write.setDstBinding(binding);
  write.setImageInfo(info);
  
  _textures[binding] = shared;
  
  _device.updateDescriptorSets(write,{});
}

void DescriptorSet::WriteTextureArray(uint32_t binding,
    const Array<Ref<Texture2D>> &textures, vk::ImageLayout layout,
    const vk::DescriptorType type) {
  std::lock_guard guard(_mutex);
  
  Array<Managed<Texture2D>> sharedTextures;
  Array<vk::DescriptorImageInfo> infos;
  
  for(auto &texture : textures) {
    const auto shared = texture.Reserve();

    if(!shared->IsUploaded()) {
      shared->Upload();
    }
    
    sharedTextures.push(shared);
    infos.emplace_back(shared->GetSampler(),shared->GetGpuData().Reserve()->view,layout);
  }
  
  vk::WriteDescriptorSet write{};
  write.setDstSet(_set);
  write.setDescriptorType(type);
  write.setDstBinding(binding);
  write.setImageInfo(infos);

  _textureArrays[binding] = sharedTextures;
  
  _device.updateDescriptorSets(write,{});
}

DescriptorLayoutBuilder & DescriptorLayoutBuilder::AddBinding(const uint32_t binding,
                                                              const vk::DescriptorType type,const vk::ShaderStageFlags stages,uint32_t count) {
  if(bindings.contains(binding)) {
    utils::vassert(bindings[binding].descriptorType == type,"HOW HAVE YOU DONE THIS!!!");
    const auto existing = bindings.find(binding);
    existing->second.setStageFlags(existing->second.stageFlags | stages);
    return *this;
  }
  
  auto newBinding = vk::DescriptorSetLayoutBinding(binding,type,count,stages);
  bindings.emplace(binding,newBinding);
  return *this;
}

DescriptorLayoutBuilder & DescriptorLayoutBuilder::Clear() {
  bindings.clear();
  return *this;
}

vk::DescriptorSetLayout DescriptorLayoutBuilder::Build(const vk::Device device) {
  std::vector<vk::DescriptorSetLayoutBinding> bindingsArr;
  
  Array<vk::DescriptorBindingFlags> flags;
  
  for(auto val : bindings | std::views::values) {
    bindingsArr.push_back(val);
    flags.push_back(vk::DescriptorBindingFlagBits::eUpdateAfterBind);
  }
  
  const vk::DescriptorSetLayoutBindingFlagsCreateInfo pNext{flags};
  
  const auto info = vk::DescriptorSetLayoutCreateInfo(vk::DescriptorSetLayoutCreateFlagBits::eUpdateAfterBindPool,bindingsArr,&pNext);

  return device.createDescriptorSetLayout(info);
}

// void DescriptorAllocator::Init(const vk::Device device, const uint32_t maxSets,
//                                const std::span<PoolSizeRatio> poolRatios) {
//   _device = device;
//   Array<vk::DescriptorPoolSize> poolSizes;
//   for(const auto ratio: poolRatios) {
//     vk::DescriptorPoolSize poolSize{ratio.type,static_cast<uint32_t>(ratio.ratio * maxSets)};
//     poolSizes.Push(poolSize);
//   }
//
//   const vk::DescriptorPoolCreateInfo poolInfo{{},maxSets,poolSizes};
//
//   pool = device.createDescriptorPool(poolInfo);
// }
//
// void DescriptorAllocator::Clear() const {
//   _device.resetDescriptorPool(pool);
// }
//
// void DescriptorAllocator::Destroy() const {
//   Clear();
//   _device.destroyDescriptorPool(pool);
// }
//
// WeakPointer<vk::DescriptorSet> DescriptorAllocator::Allocate(
//     vk::DescriptorSetLayout layout) const {
//   const vk::DescriptorSetAllocateInfo allocInfo{pool,{layout}};
//   auto createdSet = _device.allocateDescriptorSets(allocInfo).at(0);
//   auto ptr = std::shared_ptr<vk::DescriptorSet>(&createdSet,[](vk::DescriptorSet * ptr) {
//     /* NO NEED SINCE IT IS NOT A PTR */
//   });
//   return ptr;
// }
//
// void DescriptorWriter::WriteImage(const uint32_t binding, vk::ImageView image,
//                                   vk::Sampler sampler, vk::ImageLayout layout, const vk::DescriptorType type) {
//   Array<vk::DescriptorImageInfo> info;
//   info.emplace_back(sampler,image,layout);
//
//   _imageInfos.Push({binding,info,type});
// }
//
// void DescriptorWriter::WriteTexture(const uint32_t binding, const std::weak_ptr<Texture> &texture,vk::ImageLayout layout,const vk::DescriptorType type) {
//   if(!texture.lock()->IsUploaded()) {
//     texture.lock()->Upload();
//   }
//
//   Array<vk::DescriptorImageInfo> info;
//   info.emplace_back(texture.lock()->GetSampler(),texture.lock()->GetGpuData().value().view,layout);
//
//   _imageInfos.emplace_back(binding,info,type);
// }
//
// void DescriptorWriter::WriteTextures(const uint32_t binding, const Array<std::weak_ptr<Texture>> &textures,vk::ImageLayout layout,const vk::DescriptorType type) {
//   Array<vk::DescriptorImageInfo> info;
//   
//   for(const auto &texture : textures) {
//     if(!texture.lock()->IsUploaded()) {
//       texture.lock()->Upload();
//     }
//     info.emplace_back(texture.lock()->GetSampler(),texture.lock()->GetGpuData().value().view,layout);
//   }
//
//   _imageInfos.emplace_back(binding,info,type);
//   
// }
//
// void DescriptorWriter::WriteBuffer(const uint32_t binding, vk::Buffer buffer, size_t size,
//                                    size_t offset, const vk::DescriptorType type) {
//
//   Array<vk::DescriptorBufferInfo> info;
//   info.emplace_back(buffer,offset,size);
//
//   _bufferInfos.emplace_back(binding,info,type);
//
// }
//
// void DescriptorWriter::Clear() {
//   _imageInfos.clear();
//   _bufferInfos.clear();
// }
//
// void DescriptorWriter::UpdateSet(const vk::Device device, const std::weak_ptr<vk::DescriptorSet> &set) {
//   Array<vk::WriteDescriptorSet> newWrites;
//   // for(auto &write : writes) {
//   //   write.setDstSet(*set.lock());
//   // }
//
//   for(auto &val : _imageInfos) {
//     vk::WriteDescriptorSet write{};
//     write.setDstSet(*set.lock());
//     write.setDstBinding(std::get<0>(val));
//     write.setDescriptorType(std::get<2>(val));
//     write.setImageInfo(std::get<1>(val));
//     newWrites.push_back(write);
//   }
//
//   for(auto &val : _bufferInfos) {
//     vk::WriteDescriptorSet write{};
//     write.setDstSet(*set.lock());
//     write.setDstBinding(std::get<0>(val));
//     write.setDescriptorType(std::get<2>(val));
//     write.setBufferInfo(std::get<1>(val));
//     newWrites.push_back(write);
//   }
//
//   device.updateDescriptorSets(newWrites,{});
// }

void DescriptorAllocatorGrowable::Init(const vk::Device device, const uint32_t maxSets,
                                       const std::span<PoolSizeRatio> poolRatios) {
  _device = device;
  _ratios.clear();
  for(auto r : poolRatios) {
    _ratios.push(r);
  }

  auto newPool = CreatePool(maxSets,poolRatios);

  _setsPerPool = maxSets * 1.5; // Grow it next alloc

  _readyPools.push(newPool);
}

void DescriptorAllocatorGrowable::ClearPools() {
  for(const auto p : _readyPools) {
    ClearDescriptorsFromPool(p);
    
    _device.resetDescriptorPool(p.second);
  }

  for(auto p : _fullPools) {
    ClearDescriptorsFromPool(p);
    
    _device.resetDescriptorPool(p.second);
    _readyPools.push(p);
  }

  _fullPools.clear();
}

void DescriptorAllocatorGrowable::DestroyPools() {
  for(const auto val : _readyPools | std::views::values) {

    _device.destroyDescriptorPool(val);
    
  }
  _readyPools.clear();
  for(const auto val : _fullPools | std::views::values) {
    _device.destroyDescriptorPool(val);
  }

  _descriptorsInUse.clear();
  _fullPools.clear();
}


Ref<DescriptorSet> DescriptorAllocatorGrowable::Allocate(
    vk::DescriptorSetLayout layout) {

  // Get or create a pool
  auto poolToUse = GetPool();

  auto allocInfo = vk::DescriptorSetAllocateInfo{poolToUse.second,layout};

  Managed<DescriptorSet> descriptorSet;

  bool failed = true;
  try {
    descriptorSet = DescriptorSetToPtr(_device.allocateDescriptorSets(allocInfo).at(0));
    failed = false;
  } catch (vk::OutOfPoolMemoryError &_) {
    
  }
  catch (vk::FragmentedPoolError &_) {
    
  }

  if(failed) {
    _fullPools.push(poolToUse);
    poolToUse = GetPool();
    allocInfo.setDescriptorPool(poolToUse.second);

    descriptorSet = DescriptorSetToPtr(_device.allocateDescriptorSets(allocInfo).at(0));
  }

  if(!_descriptorsInUse.contains(poolToUse.first)) {
    
   _descriptorsInUse.insert({poolToUse.first,{}});   
  }

  _descriptorsInUse.find(poolToUse.first)->second.push(descriptorSet);
  
  _readyPools.push(poolToUse);
  
  return descriptorSet;
}

void DescriptorAllocatorGrowable::ClearDescriptorsFromPool(
    DescriptorPoolWithId pool) {
  if(_descriptorsInUse.contains(pool.first)) {
    _descriptorsInUse.erase(_descriptorsInUse.find(pool.first));
  }
}

DescriptorPoolWithId DescriptorAllocatorGrowable::GetPool() {
  DescriptorPoolWithId newPool;
  if(!_readyPools.empty()) {
    newPool = _readyPools.back();
    _readyPools.pop();
  } else {
    newPool = CreatePool(_setsPerPool,_ratios);

    _setsPerPool = _setsPerPool * 1.5;
    if(_setsPerPool > 4092) {
      _setsPerPool = 4092;
    }
  }

  return newPool;
}

DescriptorPoolWithId DescriptorAllocatorGrowable::CreatePool(const uint32_t setCount,
                                                           const std::span<PoolSizeRatio> poolRatios){
  Array<vk::DescriptorPoolSize> poolSizes;
  for(const auto ratio : poolRatios) {
    auto poolSize = vk::DescriptorPoolSize{ratio.type,static_cast<unsigned>(ratio.ratio * setCount)};
    poolSizes.push(poolSize);
  }

  const auto poolInfo = vk::DescriptorPoolCreateInfo{vk::DescriptorPoolCreateFlagBits::eUpdateAfterBind,setCount,poolSizes};
  
  return  {++_lastPoolId,_device.createDescriptorPool(poolInfo)};
}

Managed<DescriptorSet> DescriptorAllocatorGrowable::DescriptorSetToPtr(const vk::DescriptorSet &set) const {
  return Managed<DescriptorSet>(new DescriptorSet(_device, set));
}
}
