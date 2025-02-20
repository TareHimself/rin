#include "rin/rhi/graph/GraphImagePool.h"

#include "rin/rhi/GraphicsModule.h"
#include "rin/rhi/graph/GraphImageDescriptor.h"
namespace rin::rhi
{
    GraphImagePool::ProxyImage::ProxyImage(const Shared<IDeviceImage>& image, const std::function<void()>& onDispose)
    {
        _image = image;
        _onDispose = onDispose;
    }
    void GraphImagePool::ProxyImage::OnDispose()
    {
        _onDispose();
    }
    ImageFormat GraphImagePool::ProxyImage::GetFormat()
    {
        return _image->GetFormat();
    }
    vk::Extent3D GraphImagePool::ProxyImage::GetExtent()
    {
        return _image->GetExtent();
    }
    vk::Image GraphImagePool::ProxyImage::GetImage()
    {
        return _image->GetImage();
    }
    vk::ImageView GraphImagePool::ProxyImage::GetImageView()
    {
        return _image->GetImageView();
    }
    void GraphImagePool::OnFrameStart()
    {
    }
    Shared<IDeviceImage> GraphImagePool::AllocateImage(GraphImageDescriptor& descriptor, Frame* frame)
    {
        auto hash = descriptor.ComputeHashCode();
        if(images.contains(hash))
        {

            for(auto& imageInfo : images.at(hash))
            {
                if(!imageInfo->frames.contains(frame))
                {
                    imageInfo->frames.emplace(frame);
                    return shared<ProxyImage>(imageInfo->image,[imageInfo,frame]{
                        imageInfo->frames.erase(frame);
                    });
                }
            }
        }
        else
        {
            images.emplace(hash,std::vector<Shared<ImageInfo>>{});
        }
        
        auto newImage = GraphicsModule::Get()->NewImage(descriptor.extent,descriptor.format,descriptor.usage,descriptor.mips);
        auto info = shared<ImageInfo>(newImage,std::unordered_set{frame});
        images.at(hash).emplace_back(info);
        return shared<ProxyImage>(info->image,[info,frame]{
            info->frames.erase(frame);
        });
    }
}
