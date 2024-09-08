#pragma once
#include "aerox/core/Module.hpp"
#include "aerox/core/meta/MetaMacros.hpp"

namespace aerox::audio {
    class Window;

    MCLASS()
    class AudioModule : public Module
    {
        
    public:
        std::string GetName() override;
        void Startup(GRuntime* runtime) override;
        void Shutdown(GRuntime* runtime) override;
        bool IsDependentOn(Module* module) override;
    };
}
