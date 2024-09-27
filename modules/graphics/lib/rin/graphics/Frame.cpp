#include "rin/graphics/Frame.hpp"

#include "rin/graphics/GraphicsModule.hpp"

Frame::Frame(WindowRenderer* renderer)
    {
        _renderer = renderer;
        const auto graphicsModule = _renderer->GetModule();
        const auto device = graphicsModule->GetDevice();

        _commandPool = device.createCommandPool({
            vk::CommandPoolCreateFlagBits::eResetCommandBuffer, graphicsModule->GetQueueFamily()
        });

        _commandBuffer = device.allocateCommandBuffers({_commandPool, vk::CommandBufferLevel::ePrimary, 1}).front();

        _renderFence = device.createFence({vk::FenceCreateFlagBits::eSignaled});
        _renderSemaphore = device.createSemaphore({});
        _swapchainSemaphore = device.createSemaphore({});
        _allocator = DescriptorAllocator::New(4096, {
                                                  {vk::DescriptorType::eStorageImage, 1},
                                                  {vk::DescriptorType::eUniformBuffer, 1},
                                                  {vk::DescriptorType::eCombinedImageSampler, 1}
                                              });
    }

    Frame::~Frame()
    {
        _renderer->GetModule()->WaitForDeviceIdle();
        Reset();
        const auto device = _renderer->GetModule()->GetDevice();
        device.destroySemaphore(_swapchainSemaphore);
        device.destroySemaphore(_renderSemaphore);
        device.destroyFence(_renderFence);
        device.destroyCommandPool(_commandPool);
        _allocator->Dispose();
    }

    vk::Fence Frame::GetRenderFence() const
    {
        return _renderFence;
    }

    vk::Semaphore Frame::GetRenderSemaphore() const
    {
        return _renderSemaphore;
    }

    vk::Semaphore Frame::GetSwapchainSemaphore() const
    {
        return _swapchainSemaphore;
    }

    vk::CommandBuffer Frame::GetCommandBuffer() const
    {
        return _commandBuffer;
    }

    Shared<DescriptorAllocator> Frame::GetAllocator() const
    {
        return _allocator;
    }

    void Frame::WaitForLastDraw()
    {
        const auto device = _renderer->GetModule()->GetDevice();
        if (const auto r = device.waitForFences({_renderFence}, true, std::numeric_limits<uint64_t>::max()); r !=
            vk::Result::eSuccess)
        {
            throw std::runtime_error("Failed to wait for fences for frame");
        }
    }

    void Frame::Reset()
    {
        for (auto cleanup : _cleanup)
        {
            cleanup();
        }

        _cleanup.clear();
        _allocator->Reset();
        const auto device = _renderer->GetModule()->GetDevice();
        device.resetFences({_renderFence});
    }

    void Frame::AddCleanup(const std::function<void()>& cleanupFn)
    {
        _cleanup.push_back(cleanupFn);
    }
