#pragma once
#include "IGraphicsShader.h"
#include "rin/core/IDisposable.h"
#include <filesystem>
#include "CompiledShader.h"
#include "IComputeShader.h"
#include "rin/core/memory.h"
#include "rin/io/Task.h"
#include "rin/io/TaskRunner.h"

namespace rin::graphics
{
    class IShaderManager : public IDisposable
    {
        io::TaskRunner _runner{1};
    public:
        virtual IGraphicsShader * GraphicsFromFile(const std::filesystem::path& path) = 0;
        virtual IComputeShader * ComputeFromFile(const std::filesystem::path& path) = 0;
        virtual IShaderCompilationContext * CreateCompilationContext(IShader * shader) = 0;
        void HandleCompilation(IShader * shader);
        io::Task<> StartCompilation(IShader * shader);
    };

}
