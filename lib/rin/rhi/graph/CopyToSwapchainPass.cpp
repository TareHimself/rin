#include "rin/rhi/graph/CopyToSwapchainPass.h"

namespace rin::rhi
{

    CopyToSwapchainPass::CopyToSwapchainPass(const uint64_t& id)
    {
        _imageId = id;
    }
    void CopyToSwapchainPass::Configure(GraphBuilder& builder)
    {
        builder.Read(GetId(), _imageId);
    }
    void CopyToSwapchainPass::Execute(Frame* frame, CompiledGraph* graph)
    {
        auto image = graph->GetImage(_imageId);
    }
    bool CopyToSwapchainPass::IsTerminal()
    {
        return true;
    }
}
