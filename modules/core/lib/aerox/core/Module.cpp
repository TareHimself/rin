#include "aerox/core/Module.hpp"

#include "aerox/core/GRuntime.hpp"

namespace aerox
{
    void Module::RegisterRequiredModules()
    {
    
    }

    bool Module::IsSystemModule()
    {
        return true;
    }

    GRuntime* Module::GetRuntime() const
    {
        return GRuntime::Get();
    }
}

