#pragma once
#define VULKAN_HPP_DISPATCH_LOADER_DYNAMIC 1
#include "IDeviceBuffer.h"
#include "IDeviceImage.h"
#include "IShaderManager.h"
#include "ITextureManager.h"
#include "rin/core/Module.h"
#include "rin/io/IoModule.h"
#include "vk_mem_alloc.h"

namespace rin::graphics
{
    class GraphicsModule : public Module
    {
        io::IoModule * _ioModule{};
        
    protected:
        void OnDispose() override;
        void Startup(GRuntime* runtime) override;
        void Shutdown(GRuntime* runtime) override;

    public:
        std::string GetName() override;

        Shared<IDeviceImage> NewImage(const vk::Extent3D& extent, const ImageFormat& format, const vk::ImageUsageFlags& usage, bool mips = false,
                                      const std::string &debugName = "Image");

        io::Task<Shared<IDeviceImage>> NewImage(void * data,const vk::Extent3D& extent, const ImageFormat& format, const vk::ImageUsageFlags& usage, bool mips = false,
                                      const std::string &debugName = "Image");
        
        Shared<IDeviceBuffer> NewBuffer(uint64_t size,const vk::BufferUsageFlags& usage,const vk::MemoryPropertyFlags& properties,bool sequentialWrite,bool preferHost,bool mapped = false,const std::string &debugName = "Buffer");

        Shared<IDeviceBuffer> NewTransferBuffer(uint64_t size,bool sequentialWrite,const std::string &debugName = "Transfer Buffer");
        Shared<IDeviceBuffer> NewStorageBuffer(uint64_t size,bool sequentialWrite,const std::string &debugName = "Storage Buffer");
        Shared<IDeviceBuffer> NewUniformBuffer(uint64_t size,bool sequentialWrite,const std::string &debugName = "Uniform Buffer");

        ITextureManager * GetTextureManager() const;
        IShaderManager * GetShaderManager() const;

        static uint32_t DeriveMipLevels(const vk::Extent2D& extent);
        static uint32_t DeriveMipLevels(const vk::Extent3D& extent);

        static void CopyImageToImage(const vk::CommandBuffer& cmd, const vk::Image& src, const vk::Extent3D& srcExtent,
                                     const vk::Image& dst, const vk::Extent3D& dstExtent,
                                     const vk::Filter& filter = vk::Filter::eLinear);

        static vk::RenderingAttachmentInfo MakeRenderingAttachment(const Shared<IDeviceImage>& image,
                                                                   const vk::ImageLayout& layout,
                                                                   const std::optional<vk::ClearValue>& clearValue = {});
        static vk::RenderingAttachmentInfo MakeRenderingAttachment(const vk::ImageView& view, const vk::ImageLayout& layout,
                                                                   const std::optional<vk::ClearValue>& clearValue = {});
        static vk::ImageCreateInfo MakeImageCreateInfo(ImageFormat format, const vk::Extent3D& extent,
                                                       vk::ImageUsageFlags usage);
        static vk::ImageViewCreateInfo MakeImageViewCreateInfo(ImageFormat format, const vk::Image& image,
                                                               const vk::ImageAspectFlags& aspect);
        static vk::ImageViewCreateInfo MakeImageViewCreateInfo(const Shared<IDeviceImage>& image,
                                                               const vk::ImageAspectFlags& aspect);

        static void GenerateMipMaps(const vk::CommandBuffer& cmd, const Shared<IDeviceImage>& image,
                                    const vk::Extent3D& extent, const vk::Filter& filter);

        static vk::DispatchLoaderDynamic GetDispatchLoader();
    protected:
        void RegisterRequiredModules(GRuntime* runtime) override;

    public:
        bool IsDependentOn(Module* module) override;

    private:

        vk::Instance _instance{};
        vk::Device _device{};
        vk::PhysicalDevice _physicalDevice{};
        vk::Queue _graphicsQueue{};
        uint32_t _graphicsQueueIndex{0};
        vk::Queue _transferQueue{};
        uint32_t _transferQueueIndex{0};

        vk::CommandPool _immediateCommandPool{};
        vk::CommandBuffer _immediateCommandBuffer{};
        vk::Fence _immediateFence{};
        
#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
        vk::DebugUtilsMessengerEXT _debugUtilsMessengerExt{};
#endif


        VmaAllocator _allocator{};

        Shared<IShaderManager> _shaderManager{};
        Shared<ITextureManager> _textureManager{};
        
    };
}
