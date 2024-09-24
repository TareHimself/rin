#pragma once
#include "aerox/core/Module.hpp"
#include "aerox/core/meta/MetaMacros.hpp"

class Window;

MCLASS()
class AudioModule : public AeroxModule
{
        
public:
    std::string GetName() override;
    void Startup(GRuntime* runtime) override;
    void Shutdown(GRuntime* runtime) override;
    bool IsDependentOn(AeroxModule* module) override;
};
