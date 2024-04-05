#include "aerox/Engine.hpp"
#include <aerox/EngineSubsystem.hpp>

namespace aerox {

void EngineSubsystem::OnInit(Engine *engine) {
  TOwnedBy::OnInit(engine);
  InitLogger(GetName());
  GetLogger()->Info("Initializing",GetName().c_str());
}

void EngineSubsystem::OnDestroy() {
  TOwnedBy::OnDestroy();
  GetLogger()->Info("Destroying",GetName().c_str());
}
}
