#pragma once
#include "vengine/EngineSubsystem.hpp"
#include "vengine/Object.hpp"
#include <angelscript.h>
#include <vengine/fs.hpp>
#include "generated/scripting/ScriptSubsystem.reflect.hpp"

namespace vengine {
namespace scripting {
class Script;
}
}

namespace vengine {
class Engine;
}

namespace vengine::scripting {

RCLASS()
class ScriptSubsystem : public EngineSubsystem {
  asIScriptEngine * _scriptEngine = nullptr;
  std::optional<int> _messageCallbackRef;
public:
  void Init(Engine * outer) override;
  void MessageCallback(const asSMessageInfo *msg, void* param) const;
  void DebugFromScript(const std::string &in) const;
  Managed<Script> ScriptFromFile(const fs::path &path);

  asIScriptEngine *GetScriptEngine() const;
  void BeforeDestroy() override;

  String GetName() const override;
};

REFLECT_IMPLEMENT(ScriptSubsystem)
}
