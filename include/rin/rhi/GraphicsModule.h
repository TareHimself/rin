#pragma once
#ifndef RIN_VULKAN_DISPATCH_LOADER
#define VULKAN_HPP_DISPATCH_LOADER_DYNAMIC 1
#define RIN_VULKAN_DISPATCH_LOADER
#include "CommandBuffer.h"
#endif
#include "DescriptorLayoutFactory.h"
#include "IDeviceBuffer.h"
#include "IDeviceImage.h"
#include "IShaderManager.h"
#include "ITextureManager.h"
#include "SamplerFactory.h"
#include "rin/core/Module.h"
#include "rin/io/IoModule.h"
#include "vk_mem_alloc.h"

namespace rin::rhi
{
    class WindowRenderer;
    
    class GraphicsModule : public Module
    {
        
        
    protected:
        void OnDispose() override;
        void Startup(GRuntime* runtime) override;
        void Shutdown(GRuntime* runtime) override;

    public:
        std::string GetName() override;

        Shared<IDeviceImage> NewImage(const vk::Extent3D& extent, const ImageFormat& format, const vk::ImageUsageFlags& usage, bool mips = false,
                                      const std::string &debugName = "Image");

        io::Task<Shared<IDeviceImage>> NewImage(void * data,const vk::Extent3D& extent, const ImageFormat& format, const vk::ImageUsageFlags& usage, bool mips = false,const ImageFilter& mipsFilter = ImageFilter::Linear,
                                      const std::string &debugName = "Image");
        
        Shared<IDeviceBuffer> NewBuffer(uint64_t size,const vk::BufferUsageFlags& usage,const vk::MemoryPropertyFlags& properties,bool sequentialWrite,bool preferHost,bool mapped = false,const std::string &debugName = "Buffer");

        Shared<IDeviceBuffer> NewTransferBuffer(uint64_t size,bool sequentialWrite,const std::string &debugName = "Transfer Buffer");
        Shared<IDeviceBuffer> NewStorageBuffer(uint64_t size,bool sequentialWrite,const std::string &debugName = "Storage Buffer");
        Shared<IDeviceBuffer> NewUniformBuffer(uint64_t size,bool sequentialWrite,const std::string &debugName = "Uniform Buffer");

        ITextureManager * GetTextureManager() const;
        IShaderManager * GetShaderManager() const;

        void WaitDeviceIdle();

        static void SubmitToQueue(const vk::Queue& queue,const vk::Fence& fence,const std::vector<vk::CommandBufferSubmitInfo>& submits,const std::vector<vk::SemaphoreSubmitInfo>& signalSemaphores = {},const std::vector<vk::SemaphoreSubmitInfo>& waitSemaphores = {});
        
        io::Task<void> ImmediateSubmit(std::function<void(CommandBuffer&)>&& submit);

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

        static GraphicsModule * Get();

        vk::Instance GetInstance() const;
        vk::Device GetDevice() const;
        vk::PhysicalDevice GetPhysicalDevice() const;
        vk::Queue GetGraphicsQueue() const;
        vk::Queue GetTransferQueue() const;
        uint32_t GetGraphicsQueueIndex() const;
        uint32_t GetTransferQueueIndex() const;
        
        vk::SurfaceFormatKHR GetSurfaceFormat() const;
        DescriptorLayoutFactory * GetDescriptorLayoutBuilder();

        vk::Sampler GetSampler(const SamplerInfo& info);

        void Draw();

        void OnWindowCreated(io::Window * window);
        void OnWindowDestroyed(io::Window * window);

        DEFINE_DELEGATE_LIST(onRendererCreated,WindowRenderer *)
        DEFINE_DELEGATE_LIST(onRendererDestroyed,WindowRenderer *)
    private:
        Shared<io::TaskRunner> _transferRunner{};
        DescriptorLayoutFactory _layoutFactory{};
        SamplerFactory _samplerFactory{};
        vk::Instance _instance{};
        vk::Device _device{};
        vk::PhysicalDevice _physicalDevice{};
        vk::Queue _graphicsQueue{};
        uint32_t _graphicsQueueIndex{0};
        vk::Queue _transferQueue{};
        uint32_t _transferQueueIndex{0};

        io::IoModule * _ioModule{};

        

        bool _hasDedicatedTransfer = false;

        vk::CommandPool _immediateCommandPool{};
        CommandBuffer _immediateCommandBuffer{};
        vk::Fence _immediateFence{};
        
#ifndef VULKAN_HPP_DISABLE_ENHANCED_MODE
        vk::DebugUtilsMessengerEXT _debugUtilsMessengerExt{};
#endif


        VmaAllocator _allocator{};

        Shared<IShaderManager> _shaderManager{};
        Shared<ITextureManager> _textureManager{};

        std::vector<std::pair<io::Task<void>,std::function<void(CommandBuffer&)>>> _pendingSubmits{};
        std::mutex _pendingSubmitsMutex;
        std::unordered_map<io::Window *,Shared<WindowRenderer>> _renderers;
        DelegateListHandle _windowCreateHandle{};
        DelegateListHandle _windowDestroyHandle{};
    };
}
