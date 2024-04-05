#include "aerox/Object.hpp"

#include "aerox/utils.hpp"

namespace aerox {

std::string Object::GetObjectInstanceId() const {
  return _instanceId;
}

Object::Object() {
  _instanceId = utils::uuid();
}

Object::~Object() = default;

void Object::FinishDestroy() {
  
}

void Object::Destroy() {
  _destroyPending = true;
  onDestroyedDelegate->Execute();
  OnDestroy();
  _cleaner.Run();
  delete this;
}

void Object::OnDestroy() {
  
}

bool Object::IsPendingDestroy() const {
  return  _destroyPending;
}

json Object::ToJson() {
  return IMetadata::ToJson();
}
}
