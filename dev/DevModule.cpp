
#include "DevModule.h"

#include <iostream>

#include "rin/core/GRuntime.h"

void DevModule::RegisterRequiredModules(rin::GRuntime* runtime)
{
    _ioModule = runtime->RegisterModule<rin::io::IoModule>();
}

bool DevModule::IsDependentOn(Module* module)
{
    return module == _ioModule;
}

void DevModule::Startup(rin::GRuntime* runtime)
{
    runtime->onTick->Add([this](double delta)
    {
        _ioModule->Tick(delta);
    });

    auto window =  _ioModule->CreateWindow({500,500},"Test Window",rin::io::IWindow::CreateOptions().Visible(true).Resizable(true));
    auto task = _testTaskRunner.Run<std::string>([]
    {
        return "This was gotten from a task";
    });
    task->OnCompleted([](const std::string& taskCompleted)
    {
        throw std::runtime_error("Text Exception handling");
        std::cout << "Task Completed: " + taskCompleted << std::endl;
    });
    task->OnException([](const std::exception_ptr& exception)
    {
        try
        {
            std::rethrow_exception(exception);
        }
        catch (const std::exception& e)
        {
            std::cout << std::string("Task Exception: ") + e.what() << std::endl;
        }
    });
}

void DevModule::Shutdown(rin::GRuntime* runtime)
{
    
}

std::string DevModule::GetName()
{
    return "Dev Module";
}
