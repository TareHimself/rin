#pragma once
#include "aerox/core/AeroxBase.hpp"
#include <set>

#include "BoundTexture.hpp"
#include "TextureHandle.hpp"
#include "descriptors/DescriptorAllocator.hpp"

namespace aerox::graphics
{
    class TextureManager : public AeroxBase
    {
        Shared<DescriptorAllocator> _descriptorAllocator = DescriptorAllocator::New(4096,
            {
                PoolSizeRatio{
                    vk::DescriptorType::eSampledImage,
                    0.7f
                },
                PoolSizeRatio{
                    vk::DescriptorType::eSampler,
                    0.3f
                }
            }
        );
        std::atomic<int> _lastTextureId = -1;
        std::set<int> _freeTextureIndices{};
        std::vector<BoundTexture> _textures{};
        GraphicsModule * _graphicsModule{};
        Shared<DescriptorSet> _descriptorSet{};
    public:
        TextureManager();
        
       int CreateTexture(const std::vector<std::byte>& data,const vk::Extent3D& size,vk::Format format,vk::Filter filter,vk::ImageTiling tiling,bool mipMapped = true,const std::string& debugName = "Texture");
    };
}
