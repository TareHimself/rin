#include "aerox/core/Module.hpp"

#include "aerox/core/GRuntime.hpp"

void AeroxModule::RegisterRequiredModules()
{
    
}

bool AeroxModule::IsSystemModule()
{
    return true;
}

GRuntime* AeroxModule::GetRuntime() const
{
    return GRuntime::Get();
}

