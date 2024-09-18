#pragma once
#include <stdexcept>
namespace aerox::graphics
{
    class ShaderCompileError : public std::runtime_error
    {
    public:
        ShaderCompileError(const std::string& inMessage,const std::string& inShader);
    };
    
}
