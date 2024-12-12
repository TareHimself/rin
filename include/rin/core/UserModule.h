#pragma once
#include "Module.h"

namespace rin
{
    class UserModule : public Module
    {
    public:
        bool IsSystemModule() override;
    }; 
}

