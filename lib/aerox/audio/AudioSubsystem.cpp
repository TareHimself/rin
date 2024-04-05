#include "aerox/Engine.hpp"
#include "aerox/audio/LiveAudio.hpp"
#include <aerox/audio/AudioSubsystem.hpp>
#include <bass/Bass.hpp>
#include <bass/Stream.hpp>


namespace aerox::audio {

void AudioSubsystem::OnInit(Engine *outer) {
  EngineSubsystem::OnInit(outer);
  //
  bass::init(-1,44100,0,nullptr);
  bass::setConfig(bass::ConfigGVolStream,2500.0f);
}

std::shared_ptr<LiveAudio> AudioSubsystem::PlaySound2D(
    const fs::path &filePath) {
  if(auto channel = bass::createFileStream(filePath,0,BASS_SAMPLE_FLOAT)) {
    auto live = newObject<LiveAudio>(std::shared_ptr<bass::FileStream>(channel));
    liveAudio[live->GetId()] = live;
    return live;
  }

  return {};
}

String AudioSubsystem::GetName() const {
  return "audio";
}

void AudioSubsystem::OnDestroy() {
  EngineSubsystem::OnDestroy();
  bass::free();
}
}
