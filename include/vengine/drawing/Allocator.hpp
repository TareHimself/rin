#pragma once
#include "vk_mem_alloc.h"
#include "vengine/Object.hpp"
#include "vulkan/vulkan.hpp"
#include "generated/drawing/Allocator.reflect.hpp"

namespace vengine::drawing {
struct AllocatedBuffer;
struct AllocatedImage;
class DrawingSubsystem;

struct Allocation  {
  VmaAllocation _allocation;
  void * GetMappedData() const;
  operator VmaAllocation() const;
};


struct VmaAllocated {
  Allocation alloc;
  virtual void * GetMappedData() const;
};

RSTRUCT()
struct AllocatedBuffer : VmaAllocated {
  vk::Buffer buffer = nullptr;
};

REFLECT_IMPLEMENT(AllocatedBuffer)

RSTRUCT()
struct AllocatedImage :  VmaAllocated{
  vk::Image image = nullptr;
  vk::ImageView view = nullptr;
  vk::Extent3D extent;
  vk::Format format;
};

REFLECT_IMPLEMENT(AllocatedImage)

RCLASS()
class Allocator : public Object<DrawingSubsystem>{
  VmaAllocator _allocator = nullptr;
  uint64_t _images = 0;
  uint64_t _buffers = 0;
public:
  void Init(DrawingSubsystem * outer) override;

  void BeforeDestroy() override;

  Managed<AllocatedBuffer> CreateBuffer(size_t allocSize,
                                                vk::BufferUsageFlags usage,
                                                VmaMemoryUsage memoryUsage,
                                                vk::MemoryPropertyFlags
                                                requiredFlags = {},
                                                VmaAllocationCreateFlags
                                                flags = {}) const;

  Managed<AllocatedBuffer> CreateTransferCpuGpuBuffer(
      size_t size, bool randomAccess) const;

  Managed<AllocatedBuffer> CreateUniformCpuGpuBuffer(
      size_t size, bool randomAccess) const;
  
  void DestroyBuffer(const AllocatedBuffer &buffer) const;

  Managed<AllocatedImage> AllocateImage(vk::ImageCreateInfo &createInfo,
                                                const VmaMemoryUsage memoryUsage = {},
                                                const vk::MemoryPropertyFlags requiredFlags = {}) const;
  void DestroyImage(const AllocatedImage &image) const;
};

REFLECT_IMPLEMENT(Allocator)
}

