#include "rin/graphics/shaders/ShaderCompileError.hpp"

ShaderCompileError::ShaderCompileError(const std::string& inMessage, const std::string& inShader) : std::runtime_error(
    inMessage + "\n" + inShader)
{
}
