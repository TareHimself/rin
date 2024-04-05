#define UUID_SYSTEM_GENERATOR
#include "aerox/utils.hpp"
#include "aerox/audio/AudioSubsystem.hpp"

#include <uuid.h>
#include <aerox/audio/LiveAudio.hpp>

namespace aerox::audio {

LiveAudio::LiveAudio() {
  _id = to_string(uuids::uuid_system_generator{}());
  _channel = nullptr;
}

LiveAudio::LiveAudio(const std::shared_ptr<bass::Channel>& channel) {
  _id = to_string(uuids::uuid_system_generator{}());
  _channel = channel;
}

bool LiveAudio::Play() {
  utils::vassert(static_cast<bool>(_channel),"Channel is invalid");
  return _channel->Play();
}

bool LiveAudio::Pause() {
  utils::vassert(static_cast<bool>(_channel),"Channel is invalid");
  return _channel->Pause();
}

void LiveAudio::Seek(double position) {
  utils::vassert(static_cast<bool>(_channel),"Channel is invalid");
  return _channel->SetPosition(_channel->SecondsToBytes(position),bass::PosByte);
}

double LiveAudio::GetPosition() const {
  utils::vassert(static_cast<bool>(_channel),"Channel is invalid");
  return _channel->BytesToSeconds(_channel->GetPosition(bass::PosByte));
}

size_t LiveAudio::GetLength() const {
  utils::vassert(static_cast<bool>(_channel),"Channel is invalid");
  return _channel->GetLength();
}

std::string LiveAudio::GetId() const {
  return _id;
}

void LiveAudio::OnDestroy() {
  Object::OnDestroy();
  _channel.reset();
}
}
