#include <vengine/EngineSubsystem.hpp>

namespace vengine {
void EngineSubsystem::Init(Engine *outer) {
  Object::Init(outer);
  InitLogger(GetName());
}

Engine *EngineSubsystem::GetEngine() const{
  return GetOuter();
}
}
