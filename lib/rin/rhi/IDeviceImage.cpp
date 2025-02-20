#include "rin/rhi/IDeviceImage.h"

namespace rin::rhi
{
    void imageBarrier(const vk::Image& image, const vk::CommandBuffer& cmd, const vk::ImageLayout from, const vk::ImageLayout to, const ImageBarrierOptions& options)
    {
        vk::ImageMemoryBarrier2 data[] = {
            vk::ImageMemoryBarrier2{
                options.waitForStages,
                options.srcAccessFlags,
                options.nextStages,
                options.dstAccessFlags,
                from,
                to,
                {},
                {},
                image,
                options.subresourceRange
            }
        };
        cmd.pipelineBarrier2({
            {}, {}, {}, data
        });
    }
    void IDeviceImage::Barrier(const vk::CommandBuffer& cmd, const vk::ImageLayout from, const vk::ImageLayout to,
        const ImageBarrierOptions& options)
    {
        imageBarrier(GetImage(),cmd, from, to, options);
    }
}
