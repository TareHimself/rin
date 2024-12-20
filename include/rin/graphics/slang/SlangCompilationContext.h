#pragma once
#include "rin/graphics/IShaderCompilationContext.h"

namespace rin::graphics
{
    class SlangShaderManager;
}

namespace rin::graphics
{
    class SlangCompilationContext : public IShaderCompilationContext
    {
        
    public:
        SlangShaderManager * manager;
        SlangCompilationContext(SlangShaderManager * inManager);
    };
}
