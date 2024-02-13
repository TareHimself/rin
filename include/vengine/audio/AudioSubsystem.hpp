#pragma once
#include "vengine/EngineSubsystem.hpp"
#include "generated/audio/AudioSubsystem.reflect.hpp"

namespace vengine {
namespace audio {
class LiveAudio;
}
}

namespace vengine::audio {
RCLASS()
class AudioSubsystem : public EngineSubsystem {
  
public:
  std::unordered_map<std::string,Managed<LiveAudio>> liveAudio;
  void Init(Engine *outer) override;
  Managed<LiveAudio> PlaySound2D(const std::filesystem::path& filePath);
  String GetName() const override;
  void BeforeDestroy() override;
};

REFLECT_IMPLEMENT(AudioSubsystem)
}
