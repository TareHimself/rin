#pragma once
#include "vengine/Object.hpp"
#include "vengine/containers/String.hpp"
#include <angelscript.h>
#include <filesystem>

namespace vengine {
namespace scripting {
class ScriptManager;
}
}

namespace vengine::scripting {
class Script : public Object<ScriptManager> {
  std::filesystem::path _scriptPath{};
  String _moduleId{};
  asIScriptContext * _entryContext = nullptr;
  asIScriptModule * _module = nullptr;
public:
  virtual void Init(ScriptManager * outer,const std::filesystem::path &scriptPath,const String &moduleId);
  virtual void Init(ScriptManager *outer) override;

  virtual asIScriptModule * GetModule();
  virtual void InitScript();
};
}
