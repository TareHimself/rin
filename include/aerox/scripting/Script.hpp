#pragma once
#include "aerox/TOwnedBy.hpp"
#include "aerox/containers/String.hpp"
#include <vscript/vscript.hpp>
#include <aerox/fs.hpp>

namespace aerox {
    namespace scripting {
        class ScriptSubsystem;
    }
}

namespace aerox::scripting {
    class Script : public TOwnedBy<ScriptSubsystem> {
        std::shared_ptr<vs::frontend::Module> _module;
    public:
      Script(const std::shared_ptr<vs::frontend::Module>& mod);
        void OnInit(ScriptSubsystem *subsystem) override;

        virtual std::weak_ptr<vs::frontend::Module> GetModule();
        virtual void Run();
    };
}