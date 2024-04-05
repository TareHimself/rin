#include<aerox/scripting/Script.hpp>
#include <aerox/scripting/ScriptSubsystem.hpp>
#include <vscript/frontend/Error.hpp>
#include <aerox/utils.hpp>

namespace aerox::scripting {


Script::Script(const std::shared_ptr<vs::frontend::Module> &mod) {
  _module = mod;
}

void Script::OnInit(ScriptSubsystem *subsystem) {
  TOwnedBy<ScriptSubsystem>::OnInit(subsystem);
}

std::weak_ptr<vs::frontend::Module> Script::GetModule() {
  return _module;
}

void Script::Run() {
  const auto resolved = vs::frontend::resolveReference(_module->Find("main"));
  if (const auto entry = vs::cast<vs::frontend::Function>(resolved)) {
    try {
      entry->Call({});
    } catch (std::exception & e) {
      GetOwner()->GetLogger()->Error(e.what());
    }
    catch (std::shared_ptr<vs::frontend::Error>& err) {
      GetOwner()->GetLogger()->Error(err->ToString());
    }
  }
}
}
