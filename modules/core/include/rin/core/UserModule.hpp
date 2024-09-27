#pragma once
#include "Module.hpp"

class UserModule : public RinModule
{
public:

    bool IsSystemModule() override;
};