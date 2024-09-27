
#include "TestModule.hpp"
#include "rin/core/GRuntime.hpp"

int main()
{
    GRuntime::Get()->RegisterModule<TestModule>();
    GRuntime::Get()->Run();
}
