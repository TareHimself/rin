#include "vengine/Engine.hpp"
#include "vengine/audio/LiveAudio.hpp"

#include <vengine/audio/AudioSubsystem.hpp>
#include <bass/Bass.hpp>
#include <bass/Stream.hpp>

namespace vengine::audio {

void AudioSubsystem::Init(Engine *outer) {
  EngineSubsystem::Init(outer);
  //
  bass::init(-1,44100,0,nullptr);
  bass::setConfig(bass::ConfigGVolStream,2500.0f);
}

Managed<LiveAudio> AudioSubsystem::PlaySound2D(
    const std::filesystem::path &filePath) {
  if(auto channel = bass::createFileStream(filePath,0,BASS_SAMPLE_FLOAT)) {
    auto live = newManagedObject<LiveAudio>(channel);
    live->Init(this);
    liveAudio[live->GetId()] = live;
    return live;
  }

  return {};
}

String AudioSubsystem::GetName() const {
  return "audio";
}

void AudioSubsystem::BeforeDestroy() {
  EngineSubsystem::BeforeDestroy();
  bass::free();
}
}
