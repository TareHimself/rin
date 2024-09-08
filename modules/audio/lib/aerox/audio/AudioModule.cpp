#include "aerox/core/GRuntime.hpp"
#include "aerox/core/utils.hpp"
#include "aerox/audio/AudioModule.hpp"
#include "bass/Bass.hpp"
namespace aerox::audio {
    std::string AudioModule::GetName()
    {
        return "Audio Module";
    }

    void AudioModule::Startup(GRuntime* runtime)
    {
        bass::init(-1,44100,0,nullptr);
        bass::setConfig(bass::Config::GVolStream,2500.0f);
    }

    void AudioModule::Shutdown(GRuntime* runtime)
    {
        bass::stop();
    }

    bool AudioModule::IsDependentOn(Module* module)
    {
        return false;
    }
}
