#pragma once
#include "rin/rhi/IDeviceImage.h"
namespace rin::rhi
{
    class GraphImage : public IDeviceImage
    {
        
    protected:
        void OnDispose() override;
    public:
        ImageFormat GetFormat() override;
        vk::Extent3D GetExtent() override;
        vk::Image GetImage() override;
        vk::ImageView GetImageView() override;

    };
}
