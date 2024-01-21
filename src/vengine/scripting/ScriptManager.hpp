#pragma once
#include "vengine/Object.hpp"
#include <angelscript.h>

namespace vengine {
namespace scripting {
class Script;
}
}

namespace vengine {
class Engine;
}

namespace vengine::scripting {
class ScriptManager : public Object<Engine> {
  asIScriptEngine * _scriptEngine = nullptr;
  std::optional<int> _messageCallbackRef;
public:
  void Init(Engine *outer) override;
  static void MessageCallback(const asSMessageInfo *msg, void* param);
  static void DebugFromScript(const std::string &in);
  Script * ScriptFromFile(const std::filesystem::path &path);

  asIScriptEngine * GetScriptEngine() const;
  void HandleDestroy() override;
};
}
