#include "aerox/graphics/descriptors/DescriptorSet.hpp"

#include "aerox/core/GRuntime.hpp"
#include "aerox/graphics/DeviceBuffer.hpp"
#include "aerox/graphics/DeviceImage.hpp"
#include "aerox/graphics/GraphicsModule.hpp"

DescriptorSet::DescriptorSet(const vk::DescriptorSet& descriptorSet)
    {
        _set = descriptorSet;
        _device = GRuntime::Get()->GetModule<GraphicsModule>()->GetDevice();
    }

    void DescriptorSet::WriteSampledImages(uint32_t binding, const std::vector<Shared<DeviceImage>>& images,
        vk::ImageLayout layout)
    {

        std::vector<vk::DescriptorImageInfo> infos{};
        infos.reserve(images.size());
        
        std::vector<Shared<Disposable>> toStore{};
        toStore.reserve(images.size());
        
        for (auto &image : images)
        {
            infos.push_back(vk::DescriptorImageInfo{{},image->GetImageView(),layout});
            toStore.push_back(image);
        }

        auto writeSet = vk::WriteDescriptorSet{_set,binding,{},vk::DescriptorType::eSampledImage,infos};

        _device.updateDescriptorSets(writeSet,{});
        _resources.insert_or_assign(binding,toStore);
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

    void DescriptorSet::WriteBuffer(uint32_t binding, const vk::DescriptorType bufferType,
        const Shared<DeviceBuffer>& buffer,const uint32_t offset)
    {
        std::vector<vk::DescriptorBufferInfo> infos{};
        infos.push_back(vk::DescriptorBufferInfo{buffer->GetBuffer(),offset,buffer->GetSize()});

        auto writeSet = vk::WriteDescriptorSet{_set,binding,{},bufferType,{},infos};

        _device.updateDescriptorSets(1,&writeSet,0,nullptr);
        _resources.insert_or_assign(binding,std::vector<Shared<Disposable>>{buffer});
    }

    void DescriptorSet::OnDispose(bool manual)
    {
        Disposable::OnDispose(manual);
        _resources.clear();
    }

    vk::DescriptorSet DescriptorSet::GetInternalSet() const
    {
        return _set;
    }
