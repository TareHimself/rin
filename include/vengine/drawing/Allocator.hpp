#pragma once
#include "vk_mem_alloc.h"
#include "vengine/Object.hpp"
#include "vulkan/vulkan.hpp"


namespace vengine::drawing {
struct AllocatedBuffer;
struct AllocatedImage;
class Drawer;

struct Allocation  {
  VmaAllocation _allocation;
  void * GetMappedData() const;
  operator VmaAllocation() const;
};

class Allocator : public Object<Drawer>{
  VmaAllocator _allocator = nullptr;
  uint64_t _images = 0;
  uint64_t _buffers = 0;
public:
  void Init(Drawer * outer) override;

  void HandleDestroy() override;

  Pointer<AllocatedBuffer> CreateBuffer(size_t allocSize,
                                                vk::BufferUsageFlags usage,
                                                VmaMemoryUsage memoryUsage,
                                                vk::MemoryPropertyFlags
                                                requiredFlags = {},
                                                VmaAllocationCreateFlags
                                                flags = {}) const;

  Pointer<AllocatedBuffer> CreateTransferCpuGpuBuffer(
      size_t size, bool randomAccess) const;

  Pointer<AllocatedBuffer> CreateUniformCpuGpuBuffer(
      size_t size, bool randomAccess) const;
  
  void DestroyBuffer(const AllocatedBuffer &buffer) const;

  Pointer<AllocatedImage> AllocateImage(vk::ImageCreateInfo &createInfo,
                                                const VmaMemoryUsage memoryUsage = {},
                                                const vk::MemoryPropertyFlags requiredFlags = {}) const;
  void DestroyImage(const AllocatedImage &image) const;
};
}

