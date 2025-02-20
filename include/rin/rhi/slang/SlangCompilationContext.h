#pragma once
#include "rin/rhi/IShaderCompilationContext.h"

namespace rin::rhi
{
    class SlangShaderManager;
}

namespace rin::rhi
{
    class SlangCompilationContext : public IShaderCompilationContext
    {
        
    public:
        SlangShaderManager * manager;
        SlangCompilationContext(SlangShaderManager * inManager);
    };
}
