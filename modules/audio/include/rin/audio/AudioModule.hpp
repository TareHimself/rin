#pragma once
#include "rin/core/Module.hpp"
#include "rin/core/meta/MetaMacros.hpp"

class Window;

MCLASS()

class AudioModule : public RinModule
{
public:
    std::string GetName() override;
    void Startup(GRuntime* runtime) override;
    void Shutdown(GRuntime* runtime) override;
    bool IsDependentOn(RinModule* module) override;
};
