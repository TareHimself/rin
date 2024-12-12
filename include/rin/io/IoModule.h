#pragma once
#include <optional>

#include "TaskRunner.h"
#include "rin/core/Module.h"
#include "rin/io/IWindow.h"
#include "rin/core/math/Vec2.h"

struct SDL_Window;
namespace rin::io
{
    class IWindow;

    class IoModule : public Module
    {
        std::unordered_map<SDL_Window*,Shared<IWindow>> _windows{};
        //std::unordered_map<>
        TaskRunner _mainTaskRunner{};
    public:

        void Tick(double delta);

        static IoModule* Get();
        IWindow * CreateWindow(const Vec2<int>& size, const std::string& name,
            const std::optional<IWindow::CreateOptions>& options,IWindow * parent = {});

        void OnDispose() override;

        void Startup(GRuntime* runtime) override;
        void Shutdown(GRuntime* runtime) override;
        std::string GetName() override;
        bool IsDependentOn(Module* module) override;
        void RegisterRequiredModules(GRuntime* runtime) override;

        TaskRunner& GetTaskRunner();
    };
}
