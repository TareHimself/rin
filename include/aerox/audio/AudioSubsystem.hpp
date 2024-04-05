#pragma once
#include "aerox/EngineSubsystem.hpp"
#include "aerox/meta/Macro.hpp"
#include "gen/audio/AudioSubsystem.gen.hpp"


namespace aerox {
namespace audio {
class LiveAudio;
}
}

namespace aerox::audio {
META_TYPE()
class AudioSubsystem : public EngineSubsystem {
  
public:

  META_BODY()
  
  std::unordered_map<std::string,std::shared_ptr<LiveAudio>> liveAudio;
  void OnInit(Engine *outer) override;
  std::shared_ptr<LiveAudio> PlaySound2D(const fs::path& filePath);
  String GetName() const override;
  void OnDestroy() override;
};
}
