#pragma once

#include <list>
#include <queue>
#include <thread>
#include "Task.h"
#include "rin/core/memory.h"

namespace rin::io
{
    class TaskRunner
    {
        std::list<std::thread> _threads{};
        std::mutex _notifyMutex{};
        std::mutex _queueMutex{};
        std::condition_variable _cond{};
        std::queue<Shared<Task>> _tasks{};
        bool _running{true};

        
        void __RunTask(const Shared<Task>& task);

        void HandleThread();
    public:
        explicit  TaskRunner(const uint32_t& numThreads);
        explicit  TaskRunner();
        ~TaskRunner();

        void WaitAll();
        void StopAll();
        
        template<typename T,std::enable_if_t<!std::is_void_v<T>>* = nullptr>
        Shared<TaskWithResult<T>> Run(const Shared<Delegate<T>>& delegate);
        template<typename T,std::enable_if_t<!std::is_void_v<T>>* = nullptr>
        Shared<TaskWithResult<T>> Run(const std::function<T()>& func);
        template<typename T,std::enable_if_t<!std::is_void_v<T>>* = nullptr>
        Shared<TaskWithResult<T>> Run(std::function<T()>&& func);
        template <typename TClass,typename T,std::enable_if_t<!std::is_void_v<T>>* = nullptr>
        Shared<TaskWithResult<T>> Run(TClass* instance, ClassFunctionType<TClass,T> function);
        template <typename TClass,typename T,std::enable_if_t<!std::is_void_v<T>>* = nullptr>
        Shared<TaskWithResult<T>> Run(const Shared<TClass>& instance, ClassFunctionType<TClass,T> function);


        template<typename T,std::enable_if_t<std::is_void_v<T>>* = nullptr>
        Shared<TaskNoResult> Run(const Shared<Delegate<T>>& delegate);
        template<typename T,std::enable_if_t<std::is_void_v<T>>* = nullptr>
        Shared<TaskNoResult> Run(const std::function<T()>& func);
        template<typename T,std::enable_if_t<std::is_void_v<T>>* = nullptr>
        Shared<TaskNoResult> Run(std::function<T()>&& func);
        template <typename TClass,typename T,std::enable_if_t<std::is_void_v<T>>* = nullptr>
        Shared<TaskNoResult> Run(TClass* instance, ClassFunctionType<TClass,T> function);
        template <typename TClass,typename T,std::enable_if_t<std::is_void_v<T>>* = nullptr>
        Shared<TaskNoResult> Run(const Shared<TClass>& instance, ClassFunctionType<TClass,T> function);
    };

    template <typename T, std::enable_if_t<!std::is_void_v<T>>*>
    Shared<TaskWithResult<T>> TaskRunner::Run(const Shared<Delegate<T>>& delegate)
    {
        auto task = shared<TaskWithResult<T>>(delegate);
        __RunTask(task);
        return task;
    }

    template <typename T, std::enable_if_t<!std::is_void_v<T>>*>
    Shared<TaskWithResult<T>> TaskRunner::Run(const std::function<T()>& func)
    {
        return  Run(newDelegate(func));
    }

    template <typename T, std::enable_if_t<!std::is_void_v<T>>*>
    Shared<TaskWithResult<T>> TaskRunner::Run(std::function<T()>&& func)
    {
        auto delegate = newDelegate(func);
        return  Run<T>(delegate);
    }

    template <typename TClass, typename T, std::enable_if_t<!std::is_void_v<T>>*>
    Shared<TaskWithResult<T>> TaskRunner::Run(TClass* instance, ClassFunctionType<TClass, T> function)
    {
        auto delegate = newDelegate(instance,function);
        return  Run(delegate);
    }

    template <typename TClass, typename T, std::enable_if_t<!std::is_void_v<T>>*>
    Shared<TaskWithResult<T>> TaskRunner::Run(const Shared<TClass>& instance, ClassFunctionType<TClass, T> function)
    {
        auto delegate = newDelegate(instance,function);
        return  Run(delegate);
    }

    template <typename T, std::enable_if_t<std::is_void_v<T>>*>
    Shared<TaskNoResult> TaskRunner::Run(const Shared<Delegate<T>>& delegate)
    {
        auto task = shared<TaskNoResult>(delegate);
        __RunTask(task);
        return task;
    }

    template <typename T, std::enable_if_t<std::is_void_v<T>>*>
    Shared<TaskNoResult> TaskRunner::Run(const std::function<T()>& func)
    {
        auto delegate = newDelegate(func);
        return  Run(delegate);
    }

    template <typename T, std::enable_if_t<std::is_void_v<T>>*>
    Shared<TaskNoResult> TaskRunner::Run(std::function<T()>&& func)
    {
        auto delegate = newDelegate(func);
        return  Run(delegate);
    }

    template <typename TClass, typename T, std::enable_if_t<std::is_void_v<T>>*>
    Shared<TaskNoResult> TaskRunner::Run(TClass* instance, ClassFunctionType<TClass, T> function)
    {
        auto delegate = newDelegate(instance,function);
        return  Run(delegate);
    }

    template <typename TClass, typename T, std::enable_if_t<std::is_void_v<T>>*>
    Shared<TaskNoResult> TaskRunner::Run(const Shared<TClass>& instance, ClassFunctionType<TClass, T> function)
    {
        auto delegate = newDelegate(instance,function);
        return  Run(delegate);
    }
}
