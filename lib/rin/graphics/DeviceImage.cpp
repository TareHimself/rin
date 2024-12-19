#include "rin/graphics/DeviceImage.h"

namespace rin::graphics
{
    void DeviceImage::OnDispose()
    {
        
    }

    DeviceImage::DeviceImage(const VmaAllocator& allocator,const VmaAllocation& allocation, const vk::Image& image, const vk::ImageView& view,
        const vk::Extent3D& extent, const ImageFormat& format)
    {
        _allocator = allocator;
        _allocation = allocation;
        _image = image;
        _view = view;
        _extent = extent;
        _format = format;
    }

    ImageFormat DeviceImage::GetFormat()
    {
        return _format;
    }

    vk::Extent3D DeviceImage::GetExtent()
    {
        return _extent;
    }
    vk::Image DeviceImage::GetImage()
    {
        return _image;
    }

    vk::ImageView DeviceImage::GetImageView()
    {
        return _view;
    }
}
