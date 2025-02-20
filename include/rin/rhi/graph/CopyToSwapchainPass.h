#pragma once
#include "Pass.h"
namespace rin::rhi
{
    class CopyToSwapchainPass : public Pass
    {
        uint64_t _imageId;
    public:
        CopyToSwapchainPass(const uint64_t& id);
        void Configure(GraphBuilder& builder) override;
        void Execute(Frame* frame, CompiledGraph* graph) override;
        bool IsTerminal() override;

    };
}
