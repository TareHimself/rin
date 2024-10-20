#include "rin/core/GRuntime.hpp"
#include "rin/core/utils.hpp"
#include "rin/audio/AudioModule.hpp"
#include "bass/Bass.hpp"

std::string AudioModule::GetName()
{
    return "Audio Module";
}

void AudioModule::Startup(GRuntime* runtime)
{
    bass::init(-1, 44100, 0, nullptr);
    setConfig(bass::Config::GVolStream, 2500.0f);
}

void AudioModule::Shutdown(GRuntime* runtime)
{
    bass::stop();
}

bool AudioModule::IsDependentOn(RinModule* module)
{
    return false;
}
