#pragma once
#include "aerox/core/AeroxBase.hpp"
#include <set>

#include "BoundTexture.hpp"
#include "SamplerSpec.hpp"
#include "TextureHandle.hpp"
#include "descriptors/DescriptorAllocator.hpp"

namespace aerox::graphics
{
    class ResourceManager : public Disposable
    {
        
        Shared<DescriptorAllocator> _descriptorAllocator = DescriptorAllocator::New(512,
            {
                PoolSizeRatio{
                    vk::DescriptorType::eCombinedImageSampler,
                    1.0f
                }
            }
        );
        std::mutex _mutex{};
        std::set<int> _availableTextureIndices{};
        std::vector<BoundTexture> _textures{};
        GraphicsModule * _graphicsModule{};
        Shared<DescriptorSet> _descriptorSet{};
        std::unordered_map<std::string,vk::Sampler> _samplers{};
    protected:
        void UpdateTextures(const std::vector<int>& indices);
        vk::Sampler GetOrCreateSampler(const SamplerSpec& spec);
    public:
        static constexpr int MAX_TEXTURES = 512;
        ResourceManager();


        vk::DescriptorSet GetDescriptorSet() const;
        
       int CreateTexture(const std::vector<std::byte>& data,const vk::Extent3D& size,vk::Format format,vk::Filter filter,vk::SamplerAddressMode tiling,bool mipMapped = true,const std::string& debugName = "Texture");
        
        void OnDispose(bool manual) override;
    };
}
