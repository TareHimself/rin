#pragma once
#include <cstdint>
#include <vulkan/vulkan.hpp>
#include <map>

#include "GraphImageDescriptor.h"
#include "GraphMemoryDescriptor.h"
#include "rin/graphics/ImageFormat.h"

namespace rin::graphics
{
    class GraphBuilder
    {
        std::map<uint64_t,GraphImageDescriptor> _imageDescriptors{};
        std::map<uint64_t,GraphMemoryDescriptor> _imageDescriptors{};
    public:
        uint64_t CreateImage(uint32_t width,uint32_t height,ImageFormat format,const vk::ImageLayout& layout);
        uint64_t CreateStorageBuffer(size_t size);
    };

}
