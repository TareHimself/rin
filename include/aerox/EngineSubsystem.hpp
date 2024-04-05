#pragma once
#include "Object.hpp"
#include "TOwnedBy.hpp"
#include "WithLogger.hpp"
#include "containers/String.hpp"
#include "gen/EngineSubsystem.gen.hpp"

namespace aerox {
class Engine;
}

namespace aerox {
META_TYPE(Super=Object)
class EngineSubsystem : public TOwnedBy<Engine>, public WithLogger {
  
public:

  META_BODY()
  
  void OnInit(Engine * engine) override;
  void OnDestroy() override;
  virtual String GetName() const = 0;
};
}

