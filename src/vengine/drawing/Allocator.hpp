#pragma once
#include "vk_mem_alloc.h"
#include "vengine/Object.hpp"
#include "vulkan/vulkan.hpp"

namespace vengine {
namespace drawing {
class Drawer;
}
}

namespace vengine::drawing {
struct AllocatedBuffer;
struct AllocatedImage;

struct Allocation  {
  VmaAllocation _allocation;
  void * GetMappedData() const;
  operator VmaAllocation() const;
};
class Allocator : public Object<Drawer>{
  VmaAllocator _allocator;
public:
  void Init(Drawer *outer) override;

  void HandleDestroy() override;

  AllocatedBuffer CreateBuffer(size_t allocSize, vk::BufferUsageFlags usage,
                               VmaMemoryUsage memoryUsage, vk::MemoryPropertyFlags requiredFlags = {}, VmaAllocationCreateFlags
                               flags = {}) const;

  AllocatedBuffer CreateTransferCpuGpuBuffer(size_t size,bool randomAccess) const;

  AllocatedBuffer CreateUniformCpuGpuBuffer(size_t size,bool randomAccess) const;

  void DestroyBuffer(const AllocatedBuffer &buffer) const;
  
  void AllocateImage(AllocatedImage &image, vk::ImageCreateInfo &createInfo,
                     const VmaMemoryUsage memoryUsage = {},
                     const vk::MemoryPropertyFlags requiredFlags = {}) const;

  void DestroyImage(const AllocatedImage& image) const;
};
}

