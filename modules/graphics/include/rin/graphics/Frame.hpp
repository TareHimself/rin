#pragma once
#include "WindowRenderer.hpp"
#include "descriptors/DescriptorAllocator.hpp"

class Frame
{
    WindowRenderer* _renderer{};
    vk::CommandBuffer _commandBuffer;
    vk::CommandPool _commandPool;
    vk::Fence _renderFence{};
    vk::Semaphore _renderSemaphore{};
    vk::Semaphore _swapchainSemaphore{};
    Shared<DescriptorAllocator> _allocator{};
    std::vector<std::function<void()>> _cleanup{};

public:
    explicit Frame(WindowRenderer* renderer);
    ~Frame();

    vk::Fence GetRenderFence() const;

    vk::Semaphore GetRenderSemaphore() const;

    vk::Semaphore GetSwapchainSemaphore() const;

    vk::CommandBuffer GetCommandBuffer() const;

    Shared<DescriptorAllocator> GetAllocator() const;

    void WaitForLastDraw();

    void Reset();

    void AddCleanup(const std::function<void()>& cleanupFn);
};
