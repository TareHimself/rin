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
        std::queue<Shared<__Task>> _tasks{};
        bool _running{true};

        
        void __RunTask(const Shared<__Task>& task);

        void HandleThread();
    public:
        explicit  TaskRunner(const uint32_t& numThreads);
        explicit  TaskRunner();
        ~TaskRunner();

        //void WaitAll();
        void StopAll();
        
        template<typename T,std::enable_if_t<!std::is_void_v<T>>* = nullptr>
        Task<T> Run(const Shared<Delegate<T>>& delegate);
        template<typename T,std::enable_if_t<!std::is_void_v<T>>* = nullptr>
        Task<T> Run(const std::function<T()>& func);
        template<typename T,std::enable_if_t<!std::is_void_v<T>>* = nullptr>
        Task<T> Run(std::function<T()>&& func);
        template <typename TClass,typename T,std::enable_if_t<!std::is_void_v<T>>* = nullptr>
        Task<T> Run(TClass* instance, ClassFunctionType<TClass,T> function);
        template <typename TClass,typename T,std::enable_if_t<!std::is_void_v<T>>* = nullptr>
        Task<T> Run(const Shared<TClass>& instance, ClassFunctionType<TClass,T> function);


        template<typename T,std::enable_if_t<std::is_void_v<T>>* = nullptr>
        Task<> Run(const Shared<Delegate<T>>& delegate);
        template<typename T,std::enable_if_t<std::is_void_v<T>>* = nullptr>
        Task<> Run(const std::function<T()>& func);
        template<typename T,std::enable_if_t<std::is_void_v<T>>* = nullptr>
        Task<> Run(std::function<T()>&& func);
        template <typename TClass,typename T,std::enable_if_t<std::is_void_v<T>>* = nullptr>
        Task<> Run(TClass* instance, ClassFunctionType<TClass,T> function);
        template <typename TClass,typename T,std::enable_if_t<std::is_void_v<T>>* = nullptr>
        Task<> Run(const Shared<TClass>& instance, ClassFunctionType<TClass,T> function);
    };

    template <typename T, std::enable_if_t<!std::is_void_v<T>>*>
    Task<T> TaskRunner::Run(const Shared<Delegate<T>>& delegate)
    {
        auto task = shared<__TaskWithResult<T>>(delegate);
        __RunTask(task);
        return task;
    }

    template <typename T, std::enable_if_t<!std::is_void_v<T>>*>
    Task<T> TaskRunner::Run(const std::function<T()>& func)
    {
        return  Run(newDelegate(func));
    }

    template <typename T, std::enable_if_t<!std::is_void_v<T>>*>
    Task<T> TaskRunner::Run(std::function<T()>&& func)
    {
        auto delegate = newDelegate(func);
        return  Run<T>(delegate);
    }

    template <typename TClass, typename T, std::enable_if_t<!std::is_void_v<T>>*>
    Task<T> TaskRunner::Run(TClass* instance, ClassFunctionType<TClass, T> function)
    {
        auto delegate = newDelegate(instance,function);
        return  Run(delegate);
    }

    template <typename TClass, typename T, std::enable_if_t<!std::is_void_v<T>>*>
    Task<T> TaskRunner::Run(const Shared<TClass>& instance, ClassFunctionType<TClass, T> function)
    {
        auto delegate = newDelegate(instance,function);
        return  Run(delegate);
    }

    template <typename T, std::enable_if_t<std::is_void_v<T>>*>
    Task<> TaskRunner::Run(const Shared<Delegate<T>>& delegate)
    {
        auto task = shared<__TaskNoResult>(delegate);
        __RunTask(task);
        return task;
    }

    template <typename T, std::enable_if_t<std::is_void_v<T>>*>
    Task<> TaskRunner::Run(const std::function<T()>& func)
    {
        auto delegate = newDelegate(func);
        return  Run(delegate);
    }

    template <typename T, std::enable_if_t<std::is_void_v<T>>*>
    Task<> TaskRunner::Run(std::function<T()>&& func)
    {
        auto delegate = newDelegate(func);
        return  Run(std::static_pointer_cast<Delegate<T>>(delegate));
    }

    template <typename TClass, typename T, std::enable_if_t<std::is_void_v<T>>*>
    Task<> TaskRunner::Run(TClass* instance, ClassFunctionType<TClass, T> function)
    {
        auto delegate = newDelegate(instance,function);
        return  Run(delegate);
    }

    template <typename TClass, typename T, std::enable_if_t<std::is_void_v<T>>*>
    Task<> TaskRunner::Run(const Shared<TClass>& instance, ClassFunctionType<TClass, T> function)
    {
        auto delegate = newDelegate(instance,function);
        return  Run(delegate);
    }
}
