#include "aerox/graphics/TextureManager.hpp"
#include "aerox/graphics/GraphicsModule.hpp"
#include "aerox/core/GRuntime.hpp"
#include "aerox/graphics/descriptors/DescriptorLayoutBuilder.hpp"

namespace aerox::graphics
{
    TextureManager::TextureManager()
    {
        _graphicsModule = GRuntime::Get()->GetModule<GraphicsModule>();
        {
            auto flags = vk::DescriptorBindingFlagBits::ePartiallyBound | vk::DescriptorBindingFlagBits::eVariableDescriptorCount | vk::DescriptorBindingFlagBits::eUpdateAfterBind;
            auto layout = DescriptorLayoutBuilder()
            .AddBinding(0,vk::DescriptorType::eSampledImage,vk::ShaderStageFlagBits::eAll,1,flags)
            .AddBinding(1,vk::DescriptorType::eSampler,vk::ShaderStageFlagBits::eAll,1,flags)
            .Build();
            _descriptorSet = _descriptorAllocator->Allocate(layout,{500,500});
            _graphicsModule->GetDevice().destroyDescriptorSetLayout(layout);
        }
    }

    int TextureManager::CreateTexture(const std::vector<std::byte>& data, const vk::Extent3D& size, vk::Format format,
                                      vk::Filter filter, vk::ImageTiling tiling, bool mipMapped, const std::string& debugName)
    {
        auto image = _graphicsModule->CreateImage(data,size,format,vk::ImageUsageFlagBits::eSampled,mipMapped,filter,debugName);
        auto boundTex = BoundTexture{image,filter,tiling,mipMapped,debugName};
        _textures.emplace_back(image,filter,tiling,mipMapped,debugName);
        auto textureId = static_cast<int>(_textures.size()) - 1;

    
        
        return textureId;
    }
}
