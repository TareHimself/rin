#pragma once
#define VULKAN_HPP_DISPATCH_LOADER_DYNAMIC 1
#include "macro.hpp"
#include <vulkan/vulkan.hpp>
#include <vk_mem_alloc.h>
#include <cstddef>
using CreateSurfaceCallback = VkSurfaceKHR(RIN_CALLBACK_CONVENTION *)(VkInstance instance);

RIN_NATIVE_API void createVulkanInstance(std::uint64_t windowHandle, VkInstance* outInstance, VkDevice* outDevice, VkPhysicalDevice* outPhysicalDevice, VkQueue* outGraphicsQueue, uint32_t* outGraphicsQueueFamily, VkQueue* outTransferQueue, uint32_t* outTransferQueueFamily, VkSurfaceKHR * outSurface, VkDebugUtilsMessengerEXT * outMessenger);

RIN_NATIVE_API void destroyVulkanMessenger(VkInstance instance,VkDebugUtilsMessengerEXT messenger);

using CreateSwapchainCallback = void(RIN_CALLBACK_CONVENTION *)(uintptr_t swapchain,void * swapchainImages,uint32_t numSwapchainImages,void * swapchainImageViews,uint32_t numSwapchainImageViews);
RIN_NATIVE_API void createSwapchain(VkDevice device,VkPhysicalDevice physicalDevice,VkSurfaceKHR surface,int swapchainFormat,int colorSpace,int presentMode,uint32_t width,uint32_t height,CreateSwapchainCallback callback);


void createBuffer(VmaAllocator allocator,VkBuffer * buffer,VmaAllocation * allocation,const size_t allocSize, const vk::BufferUsageFlags usage, const VmaMemoryUsage memoryUsage,
                  const vk::MemoryPropertyFlags requiredFlags, const VmaAllocationCreateFlags flags, const char * name);

RIN_NATIVE_API void * allocatorCreate(VkInstance instance,VkDevice device,VkPhysicalDevice physicalDevice);

RIN_NATIVE_API void allocatorDestroy(void * allocator);

RIN_NATIVE_API void allocatorNewBuffer(VkBuffer * buffer, void** allocation, size_t size, void* allocator,
                                int sequentialWrite, int preferHost, int usageFlags, int memoryPropertyFlags,
                                int mapped, const char* debugName);

RIN_NATIVE_API void allocatorNewImage(VkImage* image,void ** allocation,VkImageCreateInfo * createInfo,void * allocator, const char * debugName);

RIN_NATIVE_API void allocatorFreeBuffer(VkBuffer buffer,void * allocation,void * allocator);

RIN_NATIVE_API void allocatorFreeImage(VkImage image,void * allocation,void * allocator);

RIN_NATIVE_API void allocatorCopyToBuffer(void * allocator,void * allocation,void * data,size_t size,size_t offset);

RIN_NATIVE_API void dVkCmdBindShadersEXT(VkCommandBuffer commandBuffer,
   uint32_t stageCount, 
   VkShaderStageFlagBits* pStages, 
   VkShaderEXT* pShaders);

RIN_NATIVE_API void dVkCmdBeginRenderingKHR(VkCommandBuffer commandBuffer,
    VkRenderingInfo* pRenderingInfo);

RIN_NATIVE_API void dVkCmdSetPolygonModeEXT(VkCommandBuffer commandBuffer, VkPolygonMode polygonMode);

RIN_NATIVE_API void dVkCmdSetLogicOpEXT(VkCommandBuffer commandBuffer, VkLogicOp logicOp);

RIN_NATIVE_API void dVkCmdSetVertexInputEXT( VkCommandBuffer commandBuffer,
        uint32_t vertexBindingDescriptionCount, 
    VkVertexInputBindingDescription2EXT* pVertexBindingDescriptions, 
        uint32_t vertexAttributeDescriptionCount, 
    VkVertexInputAttributeDescription2EXT* pVertexAttributeDescriptions);

RIN_NATIVE_API void dVkCmdSetLogicOpEnableEXT(VkCommandBuffer commandBuffer, uint32_t logicOpEnable);

RIN_NATIVE_API void dVkCmdSetColorBlendEnableEXT(
    VkCommandBuffer commandBuffer, 
    uint32_t firstAttachment, 
    uint32_t attachmentCount, 
    uint32_t* pColorBlendEnables);

RIN_NATIVE_API void dVkCmdSetColorBlendEquationEXT(
    VkCommandBuffer commandBuffer, 
    uint32_t firstAttachment, 
    uint32_t attachmentCount, 
    VkColorBlendEquationEXT* pColorBlendEquations);

RIN_NATIVE_API void dVkCmdSetColorWriteMaskEXT(
    VkCommandBuffer commandBuffer, 
    uint32_t firstAttachment, 
    uint32_t attachmentCount,
    VkColorComponentFlags* pColorWriteMasks);

RIN_NATIVE_API VkResult dVkCreateShadersEXT(
        VkDevice device,
        uint32_t createInfoCount,
        VkShaderCreateInfoEXT* pCreateInfos,
        VkAllocationCallbacks* pAllocator,
        VkShaderEXT* pShaders);


RIN_NATIVE_API void dVkDestroyShaderEXT(
        VkDevice device,
        VkShaderEXT shader,
        VkAllocationCallbacks* pAllocator);

RIN_NATIVE_API void dVkCmdSetRasterizationSamplesEXT( VkCommandBuffer commandBuffer,
    VkSampleCountFlagBits rasterizationSamples);

RIN_NATIVE_API void dVkCmdSetAlphaToCoverageEnableEXT(VkCommandBuffer commandBuffer, uint32_t alphaToCoverageEnable);

RIN_NATIVE_API void dVkCmdSetAlphaToOneEnableEXT(VkCommandBuffer commandBuffer, uint32_t alphaToOneEnable);

RIN_NATIVE_API void dVkCmdSetSampleMaskEXT(
    VkCommandBuffer commandBuffer, 
    VkSampleCountFlagBits samples, 
    uint32_t* pSampleMask);
