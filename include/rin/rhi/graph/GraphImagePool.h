#pragma once
#include <unordered_map>
#include <unordered_set>
#include "GraphImageDescriptor.h"
#include "rin/core/memory.h"
#include "rin/rhi/IDeviceImage.h"
namespace rin::rhi
{
    class Frame;
}
namespace rin::rhi
{
    class GraphImagePool
    {
        //std::unordered_map<>
        struct ImageInfo
        {
            Shared<IDeviceImage> image;
            std::unordered_set<Frame *> frames{};
        };

        class ProxyImage : public IDeviceImage
        {
            Shared<IDeviceImage> _image;
            std::function<void()> _onDispose;
        public:
            ProxyImage(const Shared<IDeviceImage>& image,const std::function<void()>& onDispose);
        protected:
            void OnDispose() override;
        public:
            ImageFormat GetFormat() override;
            vk::Extent3D GetExtent() override;
            vk::Image GetImage() override;
            vk::ImageView GetImageView() override;
        };
        std::unordered_map<uint64_t,std::vector<Shared<ImageInfo>>> images{};
    public:
        void OnFrameStart();
        Shared<IDeviceImage> AllocateImage(GraphImageDescriptor& descriptor, Frame * frame);
        void OnImageDisposed(const uint64_t& id,Frame);
    };
}
