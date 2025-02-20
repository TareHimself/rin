﻿#pragma once
#include <string>
#include <vulkan/vulkan.hpp>
namespace rin::rhi
{
    struct ShaderPushConstant
    {
        std::string name{};
        vk::ShaderStageFlags stages{};
        size_t size{0};
    };
}
