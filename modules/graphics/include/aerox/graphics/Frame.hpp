#pragma once
#include "WindowRenderer.hpp"
#include "descriptors/DescriptorAllocator.hpp"

namespace aerox::graphics
{
    class Frame {

        WindowRenderer * _renderer{};
        vk::CommandBuffer _commandBuffer;
        vk::CommandPool _commandPool;
        vk::Fence _renderFence{};
        vk::Semaphore _renderSemaphore{};
        vk::Semaphore _swapchainSemaphore{};
        Shared<DescriptorAllocator> _allocator{};
    public:
        explicit Frame(WindowRenderer * renderer);
        ~Frame();

        vk::Fence GetRenderFence() const;

        vk::Semaphore GetRenderSemaphore() const;

        vk::Semaphore GetSwapchainSemaphore() const;

        vk::CommandBuffer GetCommandBuffer() const;

        void WaitForLastDraw();

        void Reset();
    };
}
