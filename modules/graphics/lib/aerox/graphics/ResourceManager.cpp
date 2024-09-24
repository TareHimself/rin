#include "aerox/graphics/ResourceManager.hpp"

#include <ranges>

#include "aerox/graphics/GraphicsModule.hpp"
#include "aerox/core/GRuntime.hpp"
#include "aerox/graphics/DeviceImage.hpp"
#include "aerox/graphics/SamplerSpec.hpp"
#include "aerox/graphics/descriptors/DescriptorLayoutBuilder.hpp"

void ResourceManager::UpdateTextures(const std::vector<int>& indices)
    {
        auto set = _descriptorSet->GetInternalSet();
        std::vector<vk::WriteDescriptorSet> writes{};
        for (auto& id : indices)
        {
            auto info = _textures.at(id);

            if (!info.valid) continue;

            auto sampler = GetOrCreateSampler(SamplerSpec{info.filter, info.tiling});

            vk::DescriptorImageInfo imageInfo{
                sampler, info.image->GetImageView(), vk::ImageLayout::eShaderReadOnlyOptimal
            };

            writes.emplace_back(set, 0, static_cast<uint32_t>(id), 1, vk::DescriptorType::eCombinedImageSampler,
                                &imageInfo);
        }

        if (!writes.empty())
        {
            auto device = GraphicsModule::Get()->GetDevice();
            device.updateDescriptorSets(writes, {});
        }
    }

    vk::Sampler ResourceManager::GetOrCreateSampler(const SamplerSpec& spec)
    {
        auto id = spec.GetId();
        if (_samplers.contains(id)) return _samplers[id];

        vk::SamplerCreateInfo createInfo{{}, spec.filter, spec.filter, {}, spec.tiling, spec.tiling, spec.tiling};

        auto device = GraphicsModule::Get()->GetDevice();
        return _samplers.emplace(id, device.createSampler(createInfo)).first->second;
    }

    ResourceManager::ResourceManager()
    {
        _graphicsModule = GRuntime::Get()->GetModule<GraphicsModule>();
        {
            auto flags = vk::DescriptorBindingFlagBits::ePartiallyBound |
                vk::DescriptorBindingFlagBits::eVariableDescriptorCount |
                vk::DescriptorBindingFlagBits::eUpdateAfterBind;
            auto layout = DescriptorLayoutBuilder()
                          .AddBinding(0, vk::DescriptorType::eCombinedImageSampler, vk::ShaderStageFlagBits::eAll,
                                      MAX_TEXTURES, flags)
                          .Build();
            _descriptorSet = _descriptorAllocator->Allocate(layout, {MAX_TEXTURES});
        }

        //checkerboard image
        constexpr uint32_t black = 0x000000FF;
        constexpr uint32_t magenta = 0xFF00FFFF;
        std::vector<uint32_t> pixels{}; //for 16x16 checkerboard texture
        constexpr uint32_t pixelDim = 512;
        pixels.resize(pixelDim * pixelDim);
        // std::array<uint32_t, 16 * 16> pixels; 
        for (int x = 0; x < pixelDim; x++)
        {
            for (int y = 0; y < pixelDim; y++)
            {
                pixels[y * pixelDim + x] = ((x % 2) ^ (y % 2)) ? magenta : black;
            }
        }

        std::vector<std::byte> checkerBoardData;
        checkerBoardData.resize(pixels.size() * sizeof(uint32_t));
        memcpy(checkerBoardData.data(), pixels.data(), checkerBoardData.size());

        auto texId = CreateTexture(checkerBoardData, vk::Extent3D{pixelDim, pixelDim, 1},
                                   vk::Format::eR8G8B8A8Unorm,
                                   vk::Filter::eLinear);
    }

    vk::DescriptorSet ResourceManager::GetDescriptorSet() const
    {
        return _descriptorSet->GetInternalSet();
    }

    int ResourceManager::CreateTexture(const std::vector<std::byte>& data, const vk::Extent3D& size, vk::Format format,
                                       vk::Filter filter, vk::SamplerAddressMode tiling, bool mipMapped,
                                       const std::string& debugName)
    {
        std::lock_guard guard(_mutex);


        auto image = _graphicsModule->CreateImage(data, size, format, vk::ImageUsageFlagBits::eSampled, mipMapped,
                                                  filter, debugName);
        auto boundTex = BoundTexture{image, filter, tiling, mipMapped, debugName};
        auto textureId = -1;

        if (_availableTextureIndices.empty())
        {
            _textures.emplace_back(image, filter, tiling, mipMapped, debugName);
            textureId = static_cast<int>(_textures.size()) - 1;
        }
        else
        {
            textureId = *_availableTextureIndices.begin();
            _availableTextureIndices.erase(textureId);
            _textures[textureId] = BoundTexture{image, filter, tiling, mipMapped, debugName};
        }

        UpdateTextures({textureId});

        return textureId;
    }

    void ResourceManager::OnDispose(bool manual)
    {
        Disposable::OnDispose(manual);
        _descriptorAllocator->Dispose();
        _textures.clear();
        auto device = GraphicsModule::Get()->GetDevice();
        for (auto& sampler : _samplers | std::views::values)
        {
            device.destroySampler(sampler);
        }
        _samplers.clear();
    }
