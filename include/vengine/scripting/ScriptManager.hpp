#pragma once
#include "vengine/EngineSubsystem.hpp"
#include "vengine/Object.hpp"
#include <angelscript.h>

namespace std {
namespace filesystem {
class path;
}
}

namespace vengine {
namespace scripting {
class Script;
}
}

namespace vengine {
class Engine;
}

namespace vengine::scripting {
class ScriptManager : public EngineSubsystem {
  asIScriptEngine * _scriptEngine = nullptr;
  std::optional<int> _messageCallbackRef;
public:
  void Init(Engine * outer) override;
  void MessageCallback(const asSMessageInfo *msg, void* param) const;
  void DebugFromScript(const std::string &in) const;
  Ref<Script> ScriptFromFile(const std::filesystem::path &path);

  asIScriptEngine *GetScriptEngine() const;
  void HandleDestroy() override;

  String GetName() const override;
};
}
