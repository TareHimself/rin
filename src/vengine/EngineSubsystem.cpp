#include "EngineSubsystem.hpp"

namespace vengine {
void EngineSubsystem::Init(Engine *outer) {
  Object<Engine>::Init(outer);
  InitLogger(GetName());
}
}
