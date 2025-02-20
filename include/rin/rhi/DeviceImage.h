#pragma once
#include <vk_mem_alloc.h>

#include "IDeviceImage.h"

namespace rin::rhi
{
    class DeviceImage final : public IDeviceImage
    {
        ImageFormat _format;
        vk::Extent3D _extent;
        vk::Image _image;
        vk::ImageView _view;
        VmaAllocation _allocation;
        VmaAllocator _allocator{};
    protected:
        void OnDispose() override;

    public:
        DeviceImage(const VmaAllocator& allocator,const VmaAllocation& allocation,const vk::Image& image,const vk::ImageView& view,const vk::Extent3D& extent,const ImageFormat& format);
        ImageFormat GetFormat() override;
        vk::Extent3D GetExtent() override;
        vk::Image GetImage() override;
        vk::ImageView GetImageView() override;

        
    };
}
