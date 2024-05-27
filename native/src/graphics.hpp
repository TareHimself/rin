#pragma once
#include "window.hpp"
#include <nlohmann/json.hpp>
#include <vulkan/vulkan.hpp>
#include <vk_mem_alloc.h>
using json = nlohmann::json;


EXPORT void graphicsCreateVulkanInstance(void * inWindow, void ** outInstance,void ** outDevice,void ** outPhysicalDevice,void ** outQueue,uint32_t * outQueueFamily,uintptr_t * outSurface,uintptr_t * outMessenger);

EXPORT void graphicsDestroyVulkanMessenger(void * instance,uintptr_t messenger);


using ShaderReflectedCallback = void(__stdcall *)(char * reflected,uint32_t reflectedSize);

EXPORT void graphicsReflectShader(void * inShader,uint32_t shaderSize,const ShaderReflectedCallback reflectedCallback);

using CreateSwapchainCallback = void(__stdcall *)(uintptr_t swapchain,void * swapchainImages,uint32_t numSwapchainImages,void * swapchainImageViews,uint32_t numSwapchainImageViews);
EXPORT void graphicsCreateSwapchain(void * device,void * physicalDevice,uintptr_t surface,int swapchainFormat,int colorSpace,int presentMode,uint32_t width,uint32_t height,CreateSwapchainCallback callback);


void createBuffer(VmaAllocator allocator,VkBuffer * buffer,VmaAllocation * allocation,const size_t allocSize, const vk::BufferUsageFlags usage, const VmaMemoryUsage memoryUsage,
                  const vk::MemoryPropertyFlags requiredFlags, const VmaAllocationCreateFlags flags, const char * name);

EXPORT void * graphicsAllocatorCreate(void * instance,void * device,void * physicalDevice);

EXPORT void graphicsAllocatorDestroy(void * allocator);

EXPORT void graphicsAllocatorNewBuffer(uintptr_t* buffer, void** allocation, unsigned long size, void* allocator,
                                int sequentialWrite, int preferHost, int usageFlags, int memoryPropertyFlags,
                                int mapped, const char* debugName);

EXPORT void graphicsAllocatorNewImage(uintptr_t * image,void ** allocation,void * createInfo,void * allocator, const char * debugName);

EXPORT void graphicsAllocatorFreeBuffer(uintptr_t buffer,void * allocation,void * allocator);

EXPORT void graphicsAllocatorFreeImage(uintptr_t image,void * allocation,void * allocator);

EXPORT void graphicsAllocatorCopyToBuffer(void * allocator,void * allocation,void * data,unsigned long size,unsigned long offset);