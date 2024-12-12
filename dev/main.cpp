


#include "DevModule.h"
#include "rin/core/GRuntime.h"

int main()
{
    rin::GRuntime::Get()->RegisterModule<DevModule>();

    rin::GRuntime::Get()->Run();

    return 0;
}
