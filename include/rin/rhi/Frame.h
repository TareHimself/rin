#pragma once
#include "CommandBuffer.h"
#include "DescriptorAllocator.h"
#include "WindowRenderer.h"
namespace rin::rhi
{
    class Frame : public Disposable
    {
        vk::CommandBuffer _commandBuffer{};
        vk::CommandPool _commandPool{};
        vk::Device _device{};
        Shared<DescriptorAllocator> _descriptorAllocator{};
        vk::Fence _renderFence{};
        vk::Semaphore _renderSemaphore{};
        vk::Semaphore _swapchainSemaphore{};
        WindowRenderer * _renderer{nullptr};
        
    public:
        explicit Frame(WindowRenderer * renderer);

        vk::Fence GetRenderFence() const;
        vk::Semaphore GetRenderSemaphore() const;
        vk::Semaphore GetSwapchainSemaphore() const;
        CommandBuffer GetCommandBuffer() const;
        DescriptorAllocator * GetDescriptorAllocator() const;

        void WaitForLastDraw() const;

        void Reset();

        DEFINE_DELEGATE_LIST(onReset,Frame *);
    protected:
        void OnDispose() override;


    };
}
