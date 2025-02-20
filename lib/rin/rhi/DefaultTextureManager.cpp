#include "rin/rhi/DefaultTextureManager.h"

#include "rin/core/GRuntime.h"
#include "rin/rhi/BoundTexture.h"
#include "rin/rhi/DescriptorLayoutBuilder.h"
#include "rin/rhi/GraphicsModule.h"

namespace rin::rhi
{
    void DefaultTextureManager::OnDispose()
    {
        _allocator.DestroyPools();
        for(auto& texture : _textures)
        {
            if(texture->image)
            {
                texture->image->Dispose();
            }
        }

        _textures.clear();
    }

    DefaultTextureManager::DefaultTextureManager()
    {
        vk::DescriptorBindingFlags flags = vk::DescriptorBindingFlagBits::ePartiallyBound |
            vk::DescriptorBindingFlagBits::eVariableDescriptorCount |
            vk::DescriptorBindingFlagBits::eUpdateAfterBind;

        auto layout = DescriptorLayoutBuilder{}.AddBinding(
            0,
            vk::DescriptorType::eCombinedImageSampler,
            vk::ShaderStageFlagBits::eAll,
            RIN_MAX_TEXTURES,
            flags
        ).Build();

        _set = _allocator.Allocate(layout,{RIN_MAX_TEXTURES});
    }

    DescriptorSet* DefaultTextureManager::GetDescriptorSet()
    {
        return _set;
    }

    io::Task<int> DefaultTextureManager::CreateTexture(void* data, const vk::Extent3D& size, ImageFormat format,
        ImageFilter filter, ImageTiling tiling, bool mips, const std::string& debugName)
    {
        return GraphicsModule::Get()->NewImage(data,size,format,vk::ImageUsageFlagBits::eSampled,mips,filter,debugName)->After<int>(
            [this,filter,tiling,mips](const Shared<IDeviceImage>& image){
                int textureId;
                {
                    auto texture = shared<BoundTexture>(image,filter,tiling,mips);
                    std::lock_guard g(_texturesMutex);

                    if(_freeIds.empty())
                    {
                        _textures.push_back(texture);
                        textureId = _textures.size() - 1;
                    }
                    else
                    {
                        textureId = *_freeIds.begin();
                        _freeIds.erase(textureId);
                        _textures[textureId] = texture;
                    }

                    UpdateTextures({textureId});
                }

                return textureId;
            });
    }

    void DefaultTextureManager::FreeTextures(const std::vector<int>& textureIds)
    {
        std::lock_guard g(_texturesMutex);
        for(auto& textureId : textureIds)
        {
            auto& info = _textures.at(textureId);

            if(!info->image) continue;

            info->image->Dispose();
            info->image = {};
            _freeIds.emplace(textureId);
        }
    }

    Shared<BoundTexture> DefaultTextureManager::GetTexture(int textureId) const
    {
        if(textureId < _textures.size() && !_freeIds.contains(textureId))
        {
            if(auto texture = _textures.at(textureId); texture->image)
            {
                return texture;
            }
        }

        return {};
    }
    void DefaultTextureManager::UpdateTextures(const std::vector<int>& textureIds)
    {
        std::vector<vk::WriteDescriptorSet> writes{};

        for(auto& textureId : textureIds)
        {
            auto& info = _textures.at(textureId);

            if(!info->image) continue;

            auto sampler = GraphicsModule::Get()->GetSampler({info->tiling, info->filter});

            vk::DescriptorImageInfo imageInfo{sampler, info->image->GetImageView(), vk::ImageLayout::eShaderReadOnlyOptimal};

            writes.push_back(
                vk::WriteDescriptorSet{
                    _set->GetDescriptorSet(),
                    0,
                    static_cast<uint32_t>(textureId),
                    vk::DescriptorType::eCombinedImageSampler,
                    {imageInfo},
                    {},
                    {}
                }
            );
            _freeIds.emplace(textureId);
        }

        if(!writes.empty())
        {
            GraphicsModule::Get()->GetDevice().updateDescriptorSets(writes,{});
        }
    }


    Shared<IDeviceImage> DefaultTextureManager::GetTextureImage(int textureId)
    {
        if(auto texture = GetTexture(textureId))
        {
            return texture->image;
        }

        return {};
    }

    bool DefaultTextureManager::IsTextureIdValid(int textureId)
    {
        return static_cast<bool>(GetTexture(textureId));
    }
}
