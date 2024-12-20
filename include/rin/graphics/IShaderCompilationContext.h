#pragma once

namespace rin::graphics
{
    class IShaderManager;
    class IShaderCompilationContext
    {
    public:
        virtual ~IShaderCompilationContext() = default;
    };
}
