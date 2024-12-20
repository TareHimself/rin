#include "rin/graphics/IShaderManager.h"
#include <exception>

namespace rin::graphics
{
    void IShaderManager::HandleCompilation(IShader* shader)
    {
        auto context = CreateCompilationContext(shader);
        try
        {
            shader->Compile(context);
            delete context;
        }
        catch (...)
        {
            delete context;
            std::rethrow_exception(std::current_exception());
        }
    }

    io::Task<> IShaderManager::StartCompilation(IShader* shader)
    {
        return _runner.Run<void>([this,shader]()
        {
            return HandleCompilation(shader);
        });
    }
}
