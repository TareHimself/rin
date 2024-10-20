#include "rin/graphics/DeviceImage.hpp"

#include "rin/core/GRuntime.hpp"
#include "rin/graphics/GraphicsModule.hpp"

DeviceImage::DeviceImage(const VmaAllocator allocator, const vk::Image& image, const VmaAllocation allocation,
                         const vk::Extent3D& extent, const ImageFormat format)
{
    _allocator = allocator;
    _image = image;
    _allocation = allocation;
    _extent = extent;
    _format = format;
}

void DeviceImage::OnDispose(bool manual)
{
    Disposable::OnDispose(manual);
    vmaDestroyImage(_allocator, _image, _allocation);
    GRuntime::Get()->GetModule<GraphicsModule>()->GetDevice().destroyImageView(_imageView);
}

vk::Image DeviceImage::GetImage() const
{
    return _image;
}

ImageFormat DeviceImage::GetFormat() const
{
    return _format;
}

vk::Extent3D DeviceImage::GetExtent() const
{
    return _extent;
}

vk::ImageView DeviceImage::GetImageView() const
{
    return _imageView;
}

vk::ImageView* DeviceImage::GetImageViewRef()
{
    return &_imageView;
}

void DeviceImage::Barrier(vk::CommandBuffer cmd, vk::ImageLayout from, vk::ImageLayout to,
                          const ImageBarrierOptions& options) const
{
    GraphicsModule::ImageBarrier(cmd, _image, from, to, options);
}

void DeviceImage::CopyTo(vk::CommandBuffer cmd, const Shared<DeviceImage>& dest, vk::Filter filter) const
{
    GraphicsModule::CopyImageToImage(cmd, GetImage(), GetExtent(), dest->GetImage(), dest->GetExtent(), filter);
}

void DeviceImage::CopyTo(vk::CommandBuffer cmd, const vk::Image& dest, const vk::Extent3D& destExtent,
                         vk::Filter filter) const
{
    GraphicsModule::CopyImageToImage(cmd, GetImage(), GetExtent(), dest, destExtent, filter);
}
