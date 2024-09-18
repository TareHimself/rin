#include "aerox/graphics/shaders/ShaderCompileError.hpp"
namespace aerox::graphics
{
    ShaderCompileError::ShaderCompileError(const std::string& inMessage, const std::string& inShader) : std::runtime_error(inMessage + "\n" + inShader)
    {
        
    }
}
