#pragma once
#include <string>

#include "AeroxBase.hpp"
namespace aerox
{
    class GRuntime;

    class Module : public AeroxBase
    {

    protected:
        friend GRuntime;
        virtual void Startup(GRuntime* runtime) = 0;
        virtual void Shutdown(GRuntime* runtime) = 0;
        virtual void RegisterRequiredModules();
    public:
        virtual std::string GetName() = 0;

        virtual bool IsDependentOn(Module * module) = 0;

        virtual bool IsSystemModule();

        GRuntime * GetRuntime() const;
    };
}
