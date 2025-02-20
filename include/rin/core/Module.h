#pragma once
#include <string>

#include "Disposable.h"
#include "delegates/DelegateList.h"


namespace rin
{
    class GRuntime;
    class Module : public Disposable
    {
    protected:
        friend GRuntime;
        virtual void Startup(GRuntime* runtime) = 0;
        virtual void Shutdown(GRuntime* runtime) = 0;
        virtual void RegisterRequiredModules(GRuntime* runtime) = 0;
        void OnDispose() override;
    public:
        virtual std::string GetName() = 0;

        virtual bool IsDependentOn(Module* module) = 0;

        virtual bool IsSystemModule();

        GRuntime* GetRuntime() const;

        DEFINE_DELEGATE_LIST(beforeShutdown)
    };
}