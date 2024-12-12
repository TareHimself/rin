#include "rin/core/Module.h"

#include "rin/core/GRuntime.h"

namespace rin
{
    void Module::OnDispose()
    {
        Shutdown(GetRuntime());
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
