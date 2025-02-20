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
        std::queue<Shared<Runnable>> _tasks{};
        bool _running{true};

        
        void __RunTask(const Shared<Runnable>& task);

        void HandleThread();
    public:
        explicit  TaskRunner(const uint32_t& numThreads);
        explicit  TaskRunner();
        ~TaskRunner();

        //void WaitAll();
        void StopAll();
        
        template<typename T>
        Task<T> Enqueue(const Shared<Delegate<T>>& delegate);
        
        template<typename T>
        Task<T> Enqueue(const std::function<T()>& func);
        template<typename T>
        Task<T> Enqueue(std::function<T()>&& func);
        template <typename TClass,typename T>
        Task<T> Enqueue(TClass* instance, ClassFunctionType<TClass,T> function);
        template <typename TClass,typename T>
        Task<T> Enqueue(const Shared<TClass>& instance, ClassFunctionType<TClass,T> function);


        //
        // template<typename T,std::enable_if_t<std::is_void_v<T>>* = nullptr>
        // Task<> Run(const std::function<T()>& func);
        // template<typename T,std::enable_if_t<std::is_void_v<T>>* = nullptr>
        // Task<> Run(std::function<T()>&& func);
        // template <typename TClass,typename T,std::enable_if_t<std::is_void_v<T>>* = nullptr>
        // Task<> Run(TClass* instance, ClassFunctionType<TClass,T> function);
        // template <typename TClass,typename T,std::enable_if_t<std::is_void_v<T>>* = nullptr>
        // Task<> Run(const Shared<TClass>& instance, ClassFunctionType<TClass,T> function);
    };
    
    template <typename T>
    Task<T> TaskRunner::Enqueue(const Shared<Delegate<T>>& delegate)
    {
        auto task = shared<RunnableTask<T>>(delegate);
        __RunTask(task);
        return task;
    }

    template <typename T>
    Task<T> TaskRunner::Enqueue(const std::function<T()>& func)
    {
        return  Enqueue(std::static_pointer_cast<Delegate<T>>(newDelegate(func)));
    }

    template <typename T>
    Task<T> TaskRunner::Enqueue(std::function<T()>&& func)
    {
        auto delegate = newDelegate(func);
        return  Enqueue<T>(std::static_pointer_cast<Delegate<T>>(delegate));
    }

    template <typename TClass, typename T>
    Task<T> TaskRunner::Enqueue(TClass* instance, ClassFunctionType<TClass, T> function)
    {
        auto delegate = newDelegate(instance,function);
        return  Enqueue<T>(std::static_pointer_cast<Delegate<T>>(delegate));
    }

    template <typename TClass, typename T>
    Task<T> TaskRunner::Enqueue(const Shared<TClass>& instance, ClassFunctionType<TClass, T> function)
    {
        auto delegate = newDelegate(instance,function);
        return  Enqueue<T>(std::static_pointer_cast<Delegate<T>>(delegate));
    }
}
