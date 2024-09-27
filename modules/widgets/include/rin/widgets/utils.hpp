#pragma once
#include <vulkan/vulkan.hpp>

// Puts the stencil into write mode and disables compares
void enableStencilWrite(const vk::CommandBuffer& cmd,uint32_t writeMask,uint32_t writeValue,vk::StencilFaceFlags faceMask = vk::StencilFaceFlagBits::eFrontAndBack);

void enableStencilCompare(const vk::CommandBuffer& cmd,uint32_t compareMask,vk::CompareOp compareOp,vk::StencilFaceFlags faceMask = vk::StencilFaceFlagBits::eFrontAndBack);
