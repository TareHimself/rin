#include "rin/rhi/DeviceImage.h"

#include "rin/rhi/GraphicsModule.h"

namespace rin::rhi
{
    void DeviceImage::OnDispose()
    {
        auto device = GraphicsModule::Get()->GetDevice();
        device.destroyImageView(_view);
        vmaDestroyImage(_allocator,_image,_allocation);
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
