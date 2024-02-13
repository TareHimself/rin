#pragma once
#include "vengine/Object.hpp"
#include <bass/Channel.hpp>
#include "generated/audio/LiveAudio.reflect.hpp"
namespace vengine::audio {
class AudioSubsystem;

RCLASS()
class LiveAudio : public Object<AudioSubsystem> {
  bass::Channel * _channel = nullptr;
  std::string _id;
public:
  LiveAudio();

  LiveAudio(bass::Channel * channel);

  void Init(AudioSubsystem *outer) override;

  RFUNCTION()
  virtual bool Play();
  RFUNCTION()
  virtual bool Pause();
  RFUNCTION()
  virtual void Seek(double position);
  RFUNCTION()
  virtual double GetPosition() const;
  RFUNCTION()
  virtual size_t GetLength() const;
  
  std::string GetId() const;
  
  void BeforeDestroy() override;
};

REFLECT_IMPLEMENT(LiveAudio)
}


