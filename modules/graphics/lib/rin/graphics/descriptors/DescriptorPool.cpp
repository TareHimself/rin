#include "rin/graphics/descriptors/DescriptorPool.hpp"
#include "rin/graphics/GraphicsModule.hpp"
#include "rin/core/GRuntime.hpp"


DescriptorPool::DescriptorPool(const vk::DescriptorPool& pool)
{
    _pool = pool;
}

Shared<DescriptorSet> DescriptorPool::Allocate(const vk::DescriptorSetLayout& layout,
                                               const std::vector<uint32_t>& variableCount)
{
    auto info = vk::DescriptorSetAllocateInfo{_pool, layout};
    auto device = GRuntime::Get()->GetModule<GraphicsModule>()->GetDevice();
    if (!variableCount.empty())
    {
        auto countInfo = vk::DescriptorSetVariableDescriptorCountAllocateInfo{variableCount};
        info.pNext = &countInfo;

        auto desc = device.allocateDescriptorSets(info).at(0);
        auto allocDesc = newShared<DescriptorSet>(desc);
        _descriptors.push_back(allocDesc);
        return allocDesc;
    }

    auto desc = device.allocateDescriptorSets(info).at(0);
    auto allocDesc = newShared<DescriptorSet>(desc);
    _descriptors.push_back(allocDesc);
    return allocDesc;
}

void DescriptorPool::Reset()
{
    for (auto& descriptor : _descriptors)
    {
        descriptor->Dispose();
    }

    _descriptors.clear();

    auto device = GRuntime::Get()->GetModule<GraphicsModule>()->GetDevice();
    device.resetDescriptorPool(_pool);
}

void DescriptorPool::OnDispose(bool manual)
{
    Disposable::OnDispose(manual);

    auto device = GRuntime::Get()->GetModule<GraphicsModule>()->GetDevice();

    device.destroyDescriptorPool(_pool);

    for (auto& descriptor : _descriptors)
    {
        descriptor->Dispose();
    }

    _descriptors.clear();
}
