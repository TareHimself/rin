#include "rin/rhi/Frame.h"

namespace rin::rhi
{
    Frame::Frame(WindowRenderer* renderer)
    {
        const auto module = renderer->GetGraphicsModule();
        _renderer = renderer;
        _device = renderer->GetGraphicsModule()->GetDevice();
        _descriptorAllocator = shared<DescriptorAllocator>(1000,std::vector{
            PoolSizeRatio{vk::DescriptorType::eStorageImage,3},
            PoolSizeRatio{vk::DescriptorType::eStorageBuffer,3},
            PoolSizeRatio{vk::DescriptorType::eUniformBuffer,3},
            PoolSizeRatio{vk::DescriptorType::eCombinedImageSampler,4}
        });

        _commandPool = _device.createCommandPool({vk::CommandPoolCreateFlagBits::eResetCommandBuffer,module->GetGraphicsQueueIndex()});
        _commandBuffer = _device.allocateCommandBuffers({_commandPool,vk::CommandBufferLevel::ePrimary,1}).at(0);

        _renderFence = _device.createFence({vk::FenceCreateFlagBits::eSignaled});
        _renderSemaphore = _device.createSemaphore({});
        _swapchainSemaphore = _device.createSemaphore({});
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
    CommandBuffer Frame::GetCommandBuffer() const
    {
        return CommandBuffer{_commandBuffer};
    }
    DescriptorAllocator* Frame::GetDescriptorAllocator() const
    {
        return _descriptorAllocator.get();
    }
    
    void Frame::WaitForLastDraw() const
    {
        _renderer->GetGraphicsModule()->WaitDeviceIdle();
    }
    void Frame::Reset()
    {
        onReset->Invoke(this);
        onReset->Clear();
        _descriptorAllocator->ResetPools();
        _device.resetFences(_renderFence);
    }
    void Frame::OnDispose()
    {
        _renderer->GetGraphicsModule()->WaitDeviceIdle();
        onReset->Invoke(this);
        onReset->Clear();
        _device.destroySemaphore(_swapchainSemaphore);
        _device.destroySemaphore(_renderSemaphore);
        _device.destroyFence(_renderFence);
        _device.destroyCommandPool(_commandPool);
    }
}
