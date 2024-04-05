#pragma once
#include "aerox/Object.hpp"
#include <bass/Channel.hpp>
#include "gen/audio/LiveAudio.gen.hpp"
namespace aerox::audio {
class AudioSubsystem;

META_TYPE()
class LiveAudio : public Object {
  std::shared_ptr<bass::Channel> _channel;
  std::string _id;
public:

  META_BODY()
  
  LiveAudio();

  LiveAudio(const std::shared_ptr<bass::Channel>& channel);
  
  META_FUNCTION()
  virtual bool Play();
  META_FUNCTION()
  virtual bool Pause();
  META_FUNCTION()
  virtual void Seek(double position);
  META_FUNCTION()
  virtual double GetPosition() const;
  META_FUNCTION()
  virtual size_t GetLength() const;
  
  std::string GetId() const;
  
  void OnDestroy() override;
};
}


