#pragma once
#include "Module.hpp"
namespace aerox
{
    class UserModule : public Module
    {
    public:

        bool IsSystemModule() override;
    };
}