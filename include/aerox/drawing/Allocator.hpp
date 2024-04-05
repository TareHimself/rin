#pragma once
#include "vk_mem_alloc.h"
#include "aerox/Object.hpp"
#include "vulkan/vulkan.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/TObjectWithInit.hpp"
#include "aerox/TOwnedBy.hpp"
#include "gen/drawing/Allocator.gen.hpp"

namespace aerox::drawing {
struct AllocatedBuffer;
struct AllocatedImage;
class DrawingSubsystem;

struct VmaAllocated : meta::IMetadata {
private:
    VmaAllocator _allocator;

public:

    VmaAllocation alloc;

    VmaAllocated(VmaAllocator allocator){
        _allocator = allocator;
    }

    void Write(const void * src, size_t size, size_t offset = 0) const;

    template<typename T,typename = std::enable_if_t<std::is_pointer_v<T> == 0>>
    void Write(const T&src, size_t offset = 0){
        Write(static_cast<const void *>(&src), sizeof(src), offset);
    }

    void Read(void * dst, size_t offset = 0) const;
};

META_TYPE()
struct AllocatedBuffer : VmaAllocated {

  META_BODY()

    AllocatedBuffer(VmaAllocator allocator) : VmaAllocated(allocator){
    }
  
  vk::Buffer buffer = nullptr;
  size_t size = 0;
};

META_TYPE()
struct AllocatedImage :  VmaAllocated{

  META_BODY()

AllocatedImage(VmaAllocator allocator) : VmaAllocated(allocator){

}
  
  vk::Image image = nullptr;
  vk::ImageView view = nullptr;
  vk::Extent3D extent;
  vk::Format format;
};

META_TYPE()
class Allocator : public TOwnedBy<DrawingSubsystem> {
  
  VmaAllocator _allocator = nullptr;
  uint64_t _images = 0;
  uint64_t _buffers = 0;
public:
  META_BODY()
  
  void OnInit(DrawingSubsystem * subsystem) override;

  void OnDestroy() override;

  std::shared_ptr<AllocatedBuffer> CreateBuffer(size_t allocSize,
                                                vk::BufferUsageFlags usage,
                                                VmaMemoryUsage memoryUsage,
                                                vk::MemoryPropertyFlags
                                                requiredFlags = {},
                                                VmaAllocationCreateFlags
                                                flags = {},const std::string& name = "Buffer") const;

  std::shared_ptr<AllocatedBuffer> CreateTransferCpuGpuBuffer(
      size_t size, bool randomAccess,const std::string& name = "Transfer Buffer") const;

  std::shared_ptr<AllocatedBuffer> CreateUniformCpuGpuBuffer(
      size_t size, bool randomAccess,const std::string& name = "Uniform Buffer") const;

  template<typename T>
  std::shared_ptr<AllocatedBuffer> CreateBuffer(vk::BufferUsageFlags usage,
                                                VmaMemoryUsage memoryUsage,
                                                vk::MemoryPropertyFlags
                                                requiredFlags = {},
                                                VmaAllocationCreateFlags
                                                flags = {},const std::string& name = "Buffer") const;

  template<typename T>
  std::shared_ptr<AllocatedBuffer> CreateTransferCpuGpuBuffer(bool randomAccess,const std::string& name = "Transfer Buffer") const;

  template<typename T>
  std::shared_ptr<AllocatedBuffer> CreateUniformCpuGpuBuffer(bool randomAccess,const std::string& name = "Uniform Buffer") const;
  
  void DestroyBuffer(const AllocatedBuffer &buffer) const;

  std::shared_ptr<AllocatedImage> AllocateImage(vk::ImageCreateInfo &createInfo,
                                                const VmaMemoryUsage memoryUsage = {},
                                                const vk::MemoryPropertyFlags requiredFlags = {},const std::string& name = "Image") const;
  void DestroyImage(const AllocatedImage &image) const;
};

template <typename T> std::shared_ptr<AllocatedBuffer> Allocator::CreateBuffer(
    vk::BufferUsageFlags usage, VmaMemoryUsage memoryUsage,
    vk::MemoryPropertyFlags requiredFlags,
    VmaAllocationCreateFlags flags,const std::string& name) const {
  return CreateBuffer(sizeof(T),usage,memoryUsage,requiredFlags,flags,name);
}

template <typename T> std::shared_ptr<AllocatedBuffer> Allocator::CreateTransferCpuGpuBuffer(bool randomAccess,const std::string& name) const {
  return CreateTransferCpuGpuBuffer(sizeof(T),randomAccess,name);
}

template <typename T> std::shared_ptr<AllocatedBuffer> Allocator::CreateUniformCpuGpuBuffer(bool randomAccess,const std::string& name) const {
  return CreateUniformCpuGpuBuffer(sizeof(T),randomAccess,name);
}
}

