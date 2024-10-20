#pragma once
#include <stdexcept>

class ShaderCompileError : public std::runtime_error
{
public:
    ShaderCompileError(const std::string& inMessage, const std::string& inShader);
};
