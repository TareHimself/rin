#pragma once
#include "rin/core/RinBase.hpp"
#include <set>

#include "BoundTexture.hpp"
#include "Image.hpp"
#include "SamplerSpec.hpp"
#include "TextureHandle.hpp"
#include "descriptors/DescriptorAllocator.hpp"

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
    GraphicsModule* _graphicsModule{};
    Shared<DescriptorSet> _descriptorSet{};
    std::unordered_map<std::string, vk::Sampler> _samplers{};

protected:
    void UpdateTextures(const std::vector<int>& indices);
    vk::Sampler GetOrCreateSampler(const SamplerSpec& spec);

public:
    static constexpr int MAX_TEXTURES = 512;
    ResourceManager();


    vk::DescriptorSet GetDescriptorSet() const;

    int CreateTexture(const unsigned char* data, const vk::Extent3D& size, ImageFormat format,
                      vk::Filter filter = vk::Filter::eLinear,
                      vk::SamplerAddressMode tiling = vk::SamplerAddressMode::eRepeat, bool mipMapped = false,
                      const std::string& debugName = "Texture");

    int CreateTexture(Image<unsigned char>& image, ImageFormat format, vk::Filter filter = vk::Filter::eLinear,
                      vk::SamplerAddressMode tiling = vk::SamplerAddressMode::eRepeat, bool mipMapped = false,
                      const std::string& debugName = "Texture");

    void FreeTextures(const std::vector<int>& ids) const;

    bool IsValid(const int& textureId) const;

    bool IsValidChecked(const int& textureId);

    Shared<DeviceImage> GetTextureImage(const int& textureId);

    void OnDispose(bool manual) override;
};
