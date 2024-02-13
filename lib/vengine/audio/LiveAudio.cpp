#define UUID_SYSTEM_GENERATOR
#include "vengine/utils.hpp"
#include "vengine/audio/AudioSubsystem.hpp"

#include <uuid.h>
#include <vengine/audio/LiveAudio.hpp>

namespace vengine::audio {

LiveAudio::LiveAudio() {
  _id = to_string(uuids::uuid_system_generator{}());
  _channel = nullptr;
}

LiveAudio::LiveAudio(bass::Channel *channel) {
  _id = to_string(uuids::uuid_system_generator{}());
  _channel = channel;
}

void LiveAudio::Init(AudioSubsystem *outer) {
  Object<AudioSubsystem>::Init(outer);
}

bool LiveAudio::Play() {
  utils::vassert(_channel != nullptr,"Channel is invalid");
  return _channel->Play();
}

bool LiveAudio::Pause() {
  utils::vassert(_channel != nullptr,"Channel is invalid");
  return _channel->Pause();
}

void LiveAudio::Seek(double position) {
  utils::vassert(_channel != nullptr,"Channel is invalid");
  return _channel->SetPosition(_channel->SecondsToBytes(position),bass::PosByte);
}

double LiveAudio::GetPosition() const {
  utils::vassert(_channel != nullptr,"Channel is invalid");
  return _channel->BytesToSeconds(_channel->GetPosition(bass::PosByte));
}

size_t LiveAudio::GetLength() const {
  utils::vassert(_channel != nullptr,"Channel is invalid");
  return _channel->GetLength();
}

std::string LiveAudio::GetId() const {
  return _id;
}

void LiveAudio::BeforeDestroy() {
  Object<AudioSubsystem>::BeforeDestroy();
  if(_channel != nullptr) {
    delete _channel;
    _channel = nullptr;
  }
}
}
