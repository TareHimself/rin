#pragma once
#include "ImageBarrierOptions.hpp"
#include "ImageFormat.hpp"
#include "rin/graphics/Allocator.hpp"

class DeviceImage : public Disposable {
    VmaAllocator _allocator{};
    VmaAllocation _allocation{};
    vk::Image _image{};
    vk::ImageView _imageView{};
    vk::Extent3D _extent{};
    ImageFormat _format{};
public:
    DeviceImage(VmaAllocator allocator,const vk::Image& image,VmaAllocation allocation,const vk::Extent3D &extent,ImageFormat format);

    void OnDispose(bool manual) override;


    vk::Image GetImage() const;
    ImageFormat GetFormat() const;
    vk::Extent3D GetExtent() const;
    vk::ImageView GetImageView() const;
    vk::ImageView * GetImageViewRef();

    void Barrier(vk::CommandBuffer cmd,vk::ImageLayout from,vk::ImageLayout to,const ImageBarrierOptions& options = {}) const;

    void CopyTo(vk::CommandBuffer cmd,const Shared<DeviceImage>& dest,vk::Filter filter = vk::Filter::eLinear) const;
    void CopyTo(vk::CommandBuffer cmd,const vk::Image& dest,const vk::Extent3D& destExtent,vk::Filter filter = vk::Filter::eLinear) const;
};