#pragma once
#include "Object.hpp"
#include "WithLogger.hpp"
#include "containers/String.hpp"

namespace vengine {
class Engine;
}

namespace vengine {
class EngineSubsystem : public Object<Engine>, public WithLogger {
public:
  virtual String GetName() const = 0;
  virtual void Init(Engine *outer) override;

  virtual Engine *GetEngine() const;
};
}

