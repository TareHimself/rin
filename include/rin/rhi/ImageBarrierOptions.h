#pragma once
#include <vulkan/vulkan.hpp>

namespace rin::rhi
{
    struct ImageBarrierOptions
    {
        vk::AccessFlags2 dstAccessFlags = vk::AccessFlagBits2::eMemoryWrite | vk::AccessFlagBits2::eMemoryRead;

        vk::PipelineStageFlags2 nextStages = vk::PipelineStageFlagBits2::eAllCommands;
        vk::AccessFlags2 srcAccessFlags = vk::AccessFlagBits2::eMemoryWrite;
        vk::PipelineStageFlags2 waitForStages = vk::PipelineStageFlagBits2::eAllCommands;

        vk::ImageSubresourceRange subresourceRange = vk::ImageSubresourceRange{
            vk::ImageAspectFlagBits::eColor, 0, vk::RemainingMipLevels, 0, vk::RemainingArrayLayers
        };

        ImageBarrierOptions& DstAccess(vk::AccessFlags2 flags);
        ImageBarrierOptions& SrcAccess(vk::AccessFlags2 flags);
        ImageBarrierOptions& WaitStages(vk::PipelineStageFlags2 stages);
        ImageBarrierOptions& NextStages(vk::PipelineStageFlags2 stages);
        ImageBarrierOptions& SubResource(const vk::ImageSubresourceRange& subresource);
        ImageBarrierOptions() = default;
    };

    inline ImageBarrierOptions& ImageBarrierOptions::DstAccess(vk::AccessFlags2 flags)
    {
        dstAccessFlags = flags;
        return *this;
    }

    inline ImageBarrierOptions& ImageBarrierOptions::SrcAccess(vk::AccessFlags2 flags)
    {
        srcAccessFlags = flags;
        return *this;
    }

    inline ImageBarrierOptions& ImageBarrierOptions::WaitStages(vk::PipelineStageFlags2 stages)
    {
        waitForStages = stages;
        return *this;
    }

    inline ImageBarrierOptions& ImageBarrierOptions::NextStages(vk::PipelineStageFlags2 stages)
    {
        nextStages = stages;
        return *this;
    }

    inline ImageBarrierOptions& ImageBarrierOptions::SubResource(const vk::ImageSubresourceRange& subresource)
    {
        subresourceRange = subresource;
        return *this;
    }
}
