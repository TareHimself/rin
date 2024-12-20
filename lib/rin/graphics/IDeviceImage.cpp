#include "rin/graphics/IDeviceImage.h"

namespace rin::graphics
{
    void IDeviceImage::Barrier(const vk::CommandBuffer& cmd, vk::ImageLayout from, vk::ImageLayout to,
        const ImageBarrierOptions& options)
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
                GetImage(),
                options.subresourceRange
            }
        };
        cmd.pipelineBarrier2({
            {}, {}, {}, data
        });
    }
}
