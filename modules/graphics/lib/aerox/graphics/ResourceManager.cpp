#include "aerox/graphics/ResourceManager.hpp"

#include <ranges>

#include "aerox/graphics/GraphicsModule.hpp"
#include "aerox/core/GRuntime.hpp"
#include "aerox/graphics/DeviceImage.hpp"
#include "aerox/graphics/SamplerSpec.hpp"
#include "aerox/graphics/descriptors/DescriptorLayoutBuilder.hpp"

namespace aerox::graphics
{
    void ResourceManager::UpdateTextures(const std::vector<int>& indices)
    {
        auto set = _descriptorSet->GetInternalSet();
        std::vector<vk::WriteDescriptorSet> writes{};
        for(auto &id : indices)
        {
            auto info = _textures.at(id);
            
            if(!info.valid) continue;
            
            auto sampler = GetOrCreateSampler(SamplerSpec{info.filter,info.tiling});
            
            vk::DescriptorImageInfo imageInfo{sampler,info.image->GetImageView(),vk::ImageLayout::eShaderReadOnlyOptimal};
            
            vk::WriteDescriptorSet write{set,0,static_cast<uint32_t>(id),1,vk::DescriptorType::eCombinedImageSampler,&imageInfo};
        }

        if(!writes.empty())
        {
            auto device = GraphicsModule::Get()->GetDevice();
            device.updateDescriptorSets(writes,{});
        }
    }

    vk::Sampler ResourceManager::GetOrCreateSampler(const SamplerSpec& spec)
    {
        auto id = spec.GetId();
        if(_samplers.contains(id)) return _samplers[id];

        vk::SamplerCreateInfo createInfo{{},spec.filter,spec.filter,{},spec.tiling,spec.tiling,spec.tiling};

        auto device = GraphicsModule::Get()->GetDevice();
        return _samplers.emplace(id,device.createSampler(createInfo)).first->second;
    }

    ResourceManager::ResourceManager()
    {
        _graphicsModule = GRuntime::Get()->GetModule<GraphicsModule>();
        {
            auto flags = vk::DescriptorBindingFlagBits::ePartiallyBound | vk::DescriptorBindingFlagBits::eVariableDescriptorCount | vk::DescriptorBindingFlagBits::eUpdateAfterBind;
            auto layout = DescriptorLayoutBuilder()
            .AddBinding(0,vk::DescriptorType::eCombinedImageSampler,vk::ShaderStageFlagBits::eAll,1,flags)
            .Build();
            _descriptorSet = _descriptorAllocator->Allocate(layout,{512});
            _graphicsModule->GetDevice().destroyDescriptorSetLayout(layout);
        }
    }

    vk::DescriptorSet ResourceManager::GetDescriptorSet() const
    {
        return _descriptorSet->GetInternalSet();
    }

    int ResourceManager::CreateTexture(const std::vector<std::byte>& data, const vk::Extent3D& size, vk::Format format,
                                             vk::Filter filter, vk::SamplerAddressMode tiling, bool mipMapped, const std::string& debugName)
    {
        std::lock_guard guard(_mutex);

        
        auto image = _graphicsModule->CreateImage(data,size,format,vk::ImageUsageFlagBits::eSampled,mipMapped,filter,debugName);
        auto boundTex = BoundTexture{image,filter,tiling,mipMapped,debugName};
        auto textureId = -1;
        
        if(_availableTextureIndices.empty())
        {
            _textures.emplace_back(image,filter,tiling,mipMapped,debugName);
            textureId = static_cast<int>(_textures.size()) - 1;
        }
        else
        {
            textureId = *_availableTextureIndices.begin();
            _availableTextureIndices.erase(textureId);
            _textures[textureId] = BoundTexture{image,filter,tiling,mipMapped,debugName};
        }

        UpdateTextures({textureId});
        
        return textureId;
    }

    void ResourceManager::OnDispose(bool manual)
    {
        Disposable::OnDispose(manual);
        _descriptorAllocator->Reset();
        _descriptorAllocator->Dispose();
        auto device = GraphicsModule::Get()->GetDevice();
        for (auto& sampler : _samplers | std::views::values)
        {
            device.destroySampler(sampler);
        }
    }
}
