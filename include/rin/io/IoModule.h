#pragma once
#include <optional>

#include "TaskRunner.h"
#include "rin/core/Module.h"
#include "rin/io/Window.h"
#include "rin/core/math/Vec2.h"

struct GLFWwindow;
namespace rin::io
{
    class Window;

    class IoModule : public Module
    {
        std::unordered_map<GLFWwindow*,Shared<Window>> _windows{};
        //std::unordered_map<>
        TaskRunner _mainTaskRunner{};
    public:

        void Tick(double delta);

        static IoModule* Get();
        Window * CreateWindow(const Vec2<int>& size, const std::string& name,
            const std::optional<Window::CreateOptions>& options,Window * parent = {});

        void OnDispose() override;

        void Startup(GRuntime* runtime) override;
        void Shutdown(GRuntime* runtime) override;
        std::string GetName() override;
        bool IsDependentOn(Module* module) override;
        void RegisterRequiredModules(GRuntime* runtime) override;

        TaskRunner& GetTaskRunner();

        DEFINE_DELEGATE_LIST(onWindowCreated,Window *)
        DEFINE_DELEGATE_LIST(onWindowDestroyed,Window *)
    };
}
