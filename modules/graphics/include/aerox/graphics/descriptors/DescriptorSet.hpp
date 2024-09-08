#pragma once
#include "aerox/core/Disposable.hpp"
#include "aerox/graphics/shaders/Shader.hpp"
#include "vulkan/vulkan.hpp"

namespace aerox::graphics
{
    class DescriptorSet : public  Disposable
    {
        vk::DescriptorSet _set{};
        vk::Device _device{};
    public:
        struct Resource
        {
            
        };
        explicit DescriptorSet(const vk::DescriptorSet& descriptorSet);


        void WriteSampledImages(uint32_t binding,const std::vector<Shared<DeviceImage>>& images,vk::ImageLayout layout);

        void WriteSamplers(uint32_t binding,const std::vector<vk::Sampler>& samplers);
    };
}
