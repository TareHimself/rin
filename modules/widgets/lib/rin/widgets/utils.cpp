#include "rin/widgets/utils.hpp"

void enableStencilWrite(const vk::CommandBuffer& cmd, uint32_t writeMask, uint32_t writeValue,
                        vk::StencilFaceFlags faceMask)
{
    cmd.setStencilReference(faceMask, writeValue);
    cmd.setStencilWriteMask(faceMask, writeMask);
    cmd.setStencilCompareMask(faceMask, writeMask);
    cmd.setStencilOp(faceMask, vk::StencilOp::eKeep, vk::StencilOp::eReplace, vk::StencilOp::eKeep,
                     vk::CompareOp::eAlways);
}

void enableStencilCompare(const vk::CommandBuffer& cmd, uint32_t compareMask, vk::CompareOp compareOp,
                          vk::StencilFaceFlags faceMask)
{
    cmd.setStencilCompareMask(faceMask, compareMask);
    cmd.setStencilOp(faceMask, vk::StencilOp::eKeep, vk::StencilOp::eKeep, vk::StencilOp::eKeep, compareOp);
}
