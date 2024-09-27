#include "rin/core/Module.hpp"

#include "rin/core/GRuntime.hpp"

void RinModule::RegisterRequiredModules()
{
    
}

bool RinModule::IsSystemModule()
{
    return true;
}

GRuntime* RinModule::GetRuntime() const
{
    return GRuntime::Get();
}

