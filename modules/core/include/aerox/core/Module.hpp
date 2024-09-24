#pragma once
#include <string>

#include "AeroxBase.hpp"
#include "delegates/DelegateList.hpp"

class GRuntime;

class AeroxModule : public AeroxBase
{

protected:
    friend GRuntime;
    virtual void Startup(GRuntime* runtime) = 0;
    virtual void Shutdown(GRuntime* runtime) = 0;
    virtual void RegisterRequiredModules();
public:
    virtual std::string GetName() = 0;

    virtual bool IsDependentOn(AeroxModule * module) = 0;

    virtual bool IsSystemModule();

    GRuntime * GetRuntime() const;

    DEFINE_DELEGATE_LIST(beforeShutdown)
};
