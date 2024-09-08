
#include "TestModule.hpp"
#include "aerox/core/GRuntime.hpp"

int main()
{
    GRuntime::Get()->RegisterModule<TestModule>();
    GRuntime::Get()->Run();
}
