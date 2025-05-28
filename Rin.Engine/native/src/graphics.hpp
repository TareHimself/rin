#pragma once
#define VULKAN_HPP_DISPATCH_LOADER_DYNAMIC 1
#include "macro.hpp"
#include <vulkan/vulkan.hpp>
#include <vk_mem_alloc.h>
#include <cstddef>
using CreateSurfaceCallback = VkSurfaceKHR(RIN_CALLBACK_CONVENTION *)(VkInstance instance);

EXPORT_DECL void createVulkanInstance(void * windowHandle, VkInstance* outInstance,VkDevice* outDevice,VkPhysicalDevice* outPhysicalDevice,VkQueue* outGraphicsQueue, uint32_t* outGraphicsQueueFamily,VkQueue* outTransferQueue, uint32_t* outTransferQueueFamily,VkSurfaceKHR * outSurface,VkDebugUtilsMessengerEXT * outMessenger);

EXPORT_DECL void destroyVulkanMessenger(VkInstance instance,VkDebugUtilsMessengerEXT messenger);

using CreateSwapchainCallback = void(RIN_CALLBACK_CONVENTION *)(uintptr_t swapchain,void * swapchainImages,uint32_t numSwapchainImages,void * swapchainImageViews,uint32_t numSwapchainImageViews);
EXPORT_DECL void createSwapchain(VkDevice device,VkPhysicalDevice physicalDevice,VkSurfaceKHR surface,int swapchainFormat,int colorSpace,int presentMode,uint32_t width,uint32_t height,CreateSwapchainCallback callback);


void createBuffer(VmaAllocator allocator,VkBuffer * buffer,VmaAllocation * allocation,const size_t allocSize, const vk::BufferUsageFlags usage, const VmaMemoryUsage memoryUsage,
                  const vk::MemoryPropertyFlags requiredFlags, const VmaAllocationCreateFlags flags, const char * name);

EXPORT_DECL void * allocatorCreate(VkInstance instance,VkDevice device,VkPhysicalDevice physicalDevice);

EXPORT_DECL void allocatorDestroy(void * allocator);

EXPORT_DECL void allocatorNewBuffer(VkBuffer * buffer, void** allocation, size_t size, void* allocator,
                                int sequentialWrite, int preferHost, int usageFlags, int memoryPropertyFlags,
                                int mapped, const char* debugName);

EXPORT_DECL void allocatorNewImage(VkImage* image,void ** allocation,VkImageCreateInfo * createInfo,void * allocator, const char * debugName);

EXPORT_DECL void allocatorFreeBuffer(VkBuffer buffer,void * allocation,void * allocator);

EXPORT_DECL void allocatorFreeImage(VkImage image,void * allocation,void * allocator);

EXPORT_DECL void allocatorCopyToBuffer(void * allocator,void * allocation,void * data,size_t size,size_t offset);

EXPORT_DECL void dVkCmdBindShadersEXT(VkCommandBuffer commandBuffer, 
   uint32_t stageCount, 
   VkShaderStageFlagBits* pStages, 
   VkShaderEXT* pShaders);

EXPORT_DECL void dVkCmdBeginRenderingKHR(VkCommandBuffer commandBuffer, 
    VkRenderingInfo* pRenderingInfo);

EXPORT_DECL void dVkCmdSetPolygonModeEXT(VkCommandBuffer commandBuffer, VkPolygonMode polygonMode);

EXPORT_DECL void dVkCmdSetLogicOpEXT(VkCommandBuffer commandBuffer, VkLogicOp logicOp);

EXPORT_DECL void dVkCmdSetVertexInputEXT( VkCommandBuffer commandBuffer, 
        uint32_t vertexBindingDescriptionCount, 
    VkVertexInputBindingDescription2EXT* pVertexBindingDescriptions, 
        uint32_t vertexAttributeDescriptionCount, 
    VkVertexInputAttributeDescription2EXT* pVertexAttributeDescriptions);

EXPORT_DECL void dVkCmdSetLogicOpEnableEXT(VkCommandBuffer commandBuffer, uint32_t logicOpEnable);

EXPORT_DECL void dVkCmdSetColorBlendEnableEXT(
    VkCommandBuffer commandBuffer, 
    uint32_t firstAttachment, 
    uint32_t attachmentCount, 
    uint32_t* pColorBlendEnables);

EXPORT_DECL void dVkCmdSetColorBlendEquationEXT(
    VkCommandBuffer commandBuffer, 
    uint32_t firstAttachment, 
    uint32_t attachmentCount, 
    VkColorBlendEquationEXT* pColorBlendEquations);

EXPORT_DECL void dVkCmdSetColorWriteMaskEXT(
    VkCommandBuffer commandBuffer, 
    uint32_t firstAttachment, 
    uint32_t attachmentCount,
    VkColorComponentFlags* pColorWriteMasks);

EXPORT_DECL VkResult dVkCreateShadersEXT(
        VkDevice device,
        uint32_t createInfoCount,
        VkShaderCreateInfoEXT* pCreateInfos,
        VkAllocationCallbacks* pAllocator,
        VkShaderEXT* pShaders);


EXPORT_DECL void dVkDestroyShaderEXT(
        VkDevice device,
        VkShaderEXT shader,
        VkAllocationCallbacks* pAllocator);

EXPORT_DECL void dVkCmdSetRasterizationSamplesEXT( VkCommandBuffer commandBuffer, 
    VkSampleCountFlagBits rasterizationSamples);

EXPORT_DECL void dVkCmdSetAlphaToCoverageEnableEXT(VkCommandBuffer commandBuffer, uint32_t alphaToCoverageEnable);

EXPORT_DECL void dVkCmdSetAlphaToOneEnableEXT(VkCommandBuffer commandBuffer, uint32_t alphaToOneEnable);

EXPORT_DECL void dVkCmdSetSampleMaskEXT(
    VkCommandBuffer commandBuffer, 
    VkSampleCountFlagBits samples, 
    uint32_t* pSampleMask);
