#include "aerox/graphics/descriptors/DescriptorSet.hpp"

#include "aerox/core/GRuntime.hpp"
#include "aerox/graphics/DeviceImage.hpp"
#include "aerox/graphics/GraphicsModule.hpp"

namespace aerox::graphics
{
    DescriptorSet::DescriptorSet(const vk::DescriptorSet& descriptorSet)
    {
        _set = descriptorSet;
        _device = GRuntime::Get()->GetModule<GraphicsModule>()->GetDevice();
    }

    void DescriptorSet::WriteSampledImages(uint32_t binding, const std::vector<Shared<DeviceImage>>& images,
        vk::ImageLayout layout)
    {

        std::vector<vk::DescriptorImageInfo> infos{};
        
        for (auto &image : images)
        {
            infos.push_back(vk::DescriptorImageInfo{{},image->GetImageView(),layout});
        }

        auto writeSet = vk::WriteDescriptorSet{_set,binding,{},vk::DescriptorType::eSampledImage,infos};

        _device.updateDescriptorSets(writeSet,{});
    }

    void DescriptorSet::WriteSamplers(uint32_t binding, const std::vector<vk::Sampler>& samplers)
    {
        std::vector<vk::DescriptorImageInfo> infos{};
        
        for (auto &sampler : samplers)
        {
            infos.push_back(vk::DescriptorImageInfo{sampler});
        }

        auto writeSet = vk::WriteDescriptorSet{_set,binding,{},vk::DescriptorType::eSampler,infos};

        _device.updateDescriptorSets(writeSet,{});
    }
}
