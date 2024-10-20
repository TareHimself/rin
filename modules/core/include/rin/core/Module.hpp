﻿#pragma once
#include <string>

#include "RinBase.hpp"
#include "delegates/DelegateList.hpp"

class GRuntime;

class RinModule : public RinBase
{
protected:
    friend GRuntime;
    virtual void Startup(GRuntime* runtime) = 0;
    virtual void Shutdown(GRuntime* runtime) = 0;
    virtual void RegisterRequiredModules();

public:
    virtual std::string GetName() = 0;

    virtual bool IsDependentOn(RinModule* module) = 0;

    virtual bool IsSystemModule();

    GRuntime* GetRuntime() const;

    DEFINE_DELEGATE_LIST(beforeShutdown)
};
