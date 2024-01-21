#define VMA_IMPLEMENTATION
#include "Allocator.hpp"
#include "Drawer.hpp"
#include "VkBootstrap.h"

namespace vengine::drawing {

void * Allocation::GetMappedData() const {
  return _allocation->GetMappedData();
}

Allocation::operator VmaAllocation_T*() const {
  return _allocation;
}


void Allocator::Init(Drawer *outer) {
  Object<Drawer>::Init(outer);
  auto allocatorCreateInfo = VmaAllocatorCreateInfo{};
  allocatorCreateInfo.flags = VMA_ALLOCATOR_CREATE_BUFFER_DEVICE_ADDRESS_BIT;
  allocatorCreateInfo.device = outer->GetDevice();
  allocatorCreateInfo.physicalDevice = outer->GetPhysicalDevice();
  allocatorCreateInfo.instance = outer->GetVulkanInstance();
  allocatorCreateInfo.vulkanApiVersion = VKB_MAKE_VK_VERSION(0, 1, 3, 0);
  
  vmaCreateAllocator(&allocatorCreateInfo,&_allocator);
}

void Allocator::HandleDestroy() {
  Object<Drawer>::HandleDestroy();
  vmaDestroyAllocator(_allocator);
}

AllocatedBuffer Allocator::CreateBuffer(size_t allocSize,
    vk::BufferUsageFlags usage, VmaMemoryUsage memoryUsage,
    vk::MemoryPropertyFlags requiredFlags, VmaAllocationCreateFlags flags) const {
  const auto bufferInfo = vk::BufferCreateInfo({}, allocSize,
                                               usage);
  //vma::AllocationCreateFlagBits::eMapped
  VmaAllocationCreateInfo vmaAllocInfo = {};
  vmaAllocInfo.flags = flags;
  vmaAllocInfo.usage = memoryUsage;
  vmaAllocInfo.requiredFlags = static_cast<VkMemoryPropertyFlags>(requiredFlags);


  AllocatedBuffer newBuffer;
  const VkBufferCreateInfo vmaBufferCreateInfo = bufferInfo;
  VkBuffer rawBuffer = newBuffer.buffer;

  vmaCreateBuffer(_allocator,&vmaBufferCreateInfo,&vmaAllocInfo,&rawBuffer,&newBuffer.alloc._allocation,nullptr);
  newBuffer.buffer = rawBuffer;
  
  return newBuffer;
}

AllocatedBuffer Allocator::CreateTransferCpuGpuBuffer(size_t size,
    bool randomAccess) const {
  
  return CreateBuffer(size,
                      vk::BufferUsageFlagBits::eTransferSrc,
                      VMA_MEMORY_USAGE_AUTO_PREFER_HOST,
                      vk::MemoryPropertyFlagBits::eHostVisible |
                      vk::MemoryPropertyFlagBits::eHostCoherent,
                      VMA_ALLOCATION_CREATE_MAPPED_BIT | (randomAccess
                        ? VMA_ALLOCATION_CREATE_HOST_ACCESS_RANDOM_BIT
                        : VMA_ALLOCATION_CREATE_HOST_ACCESS_SEQUENTIAL_WRITE_BIT));
}

AllocatedBuffer Allocator::CreateUniformCpuGpuBuffer(size_t size,
    bool randomAccess) const {
  return CreateBuffer(
      size, vk::BufferUsageFlagBits::eUniformBuffer,
      VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,
      vk::MemoryPropertyFlagBits::eHostVisible,
      VMA_ALLOCATION_CREATE_MAPPED_BIT |
      (randomAccess
         ? VMA_ALLOCATION_CREATE_HOST_ACCESS_RANDOM_BIT
         : VMA_ALLOCATION_CREATE_HOST_ACCESS_SEQUENTIAL_WRITE_BIT));
}

void Allocator::DestroyBuffer(const AllocatedBuffer &buffer) const {
  vmaDestroyBuffer(_allocator,buffer.buffer,buffer.alloc);
}


void Allocator::AllocateImage(AllocatedImage &image,
    vk::ImageCreateInfo &createInfo, const VmaMemoryUsage memoryUsage,
    const vk::MemoryPropertyFlags requiredFlags) const {
  VmaAllocationCreateInfo imageAllocInfo = {};

  imageAllocInfo.usage = memoryUsage;
  imageAllocInfo.requiredFlags = static_cast<VkMemoryPropertyFlags>(requiredFlags);

  const VkImageCreateInfo vmaImageCreateInfo = createInfo;
  VkImage vmaImage = image.image;
  
  vmaCreateImage(_allocator,&vmaImageCreateInfo,&imageAllocInfo,&vmaImage,&image.alloc._allocation,nullptr);
  image.image = vmaImage;
  image.format = createInfo.format;
  image.extent = createInfo.extent;
}

void Allocator::DestroyImage(const AllocatedImage &image) const {
  GetOuter()->GetDevice().destroyImageView(image.view);
  
  vmaDestroyImage(_allocator,image.image,image.alloc);
}

}
