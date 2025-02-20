#pragma once

namespace rin::rhi
{
    class IShaderManager;
    class IShaderCompilationContext
    {
    public:
        virtual ~IShaderCompilationContext() = default;
    };
}
