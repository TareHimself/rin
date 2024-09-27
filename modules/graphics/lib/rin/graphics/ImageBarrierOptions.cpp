#include "rin/graphics/ImageBarrierOptions.hpp"
ImageBarrierOptions& ImageBarrierOptions::DstAccess(vk::AccessFlags2 flags)
{
    dstAccessFlags = flags;
    return *this;
}

ImageBarrierOptions& ImageBarrierOptions::SrcAccess(vk::AccessFlags2 flags)
{
    srcAccessFlags = flags;
    return *this;
}

ImageBarrierOptions& ImageBarrierOptions::WaitStages(vk::PipelineStageFlags2 stages)
{
    waitForStages = stages;
    return *this;
}

ImageBarrierOptions& ImageBarrierOptions::NextStages(vk::PipelineStageFlags2 stages)
{
    nextStages = stages;
    return *this;
}

ImageBarrierOptions& ImageBarrierOptions::SubResource(const vk::ImageSubresourceRange& subresource)
{
    subresourceRange = subresource;
    return *this;
}
