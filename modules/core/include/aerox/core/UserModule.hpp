#pragma once
#include "Module.hpp"

class UserModule : public AeroxModule
{
public:

    bool IsSystemModule() override;
};