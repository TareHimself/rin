#pragma once
#include <future>
#include <queue>
#include <thread>

#include "Disposable.hpp"
#include "delegates/FunctionDelegate.hpp"
#include "delegates/SharedClassFunctionDelegate.hpp"
#include "delegates/ClassFunctionDelegate.hpp"
#include "delegates/Delegate.hpp"

namespace aerox
{
    template<typename T>
    class BackgroundThread
    {
        struct Task
        {
            Shared<Delegate<T>> fn{};
            std::promise<T> promise{};
        };
        
        std::thread _thread{};
        std::queue<std::shared_ptr<Task>> _pendingTasks{};
        std::condition_variable _cond{};
        std::mutex _lock{};


        template<typename T_ = T, std::enable_if_t<std::is_void_v<T_>>* = nullptr>
        void RunTask(const std::shared_ptr<Task>& task);

        template<typename T_ = T, std::enable_if_t<!std::is_void_v<T_>>* = nullptr>
        void RunTask(const std::shared_ptr<Task>& task);
        std::string _name;
    public:
        BackgroundThread(const std::string& name = {});
        ~BackgroundThread();

        //template<typename T_ = T, std::enable_if_t<std::is_void_v<T>>* = nullptr>
        std::shared_future<T> Put(const Shared<Delegate<T>>& task);
        std::shared_future<T> Put(const std::function<T()>& func);
        std::shared_future<T> Put(std::function<T()>&& func);
        template<typename TClass>
        std::shared_future<T> Put(TClass * instance,ClassFunctionType<TClass,T> function);
        template<typename TClass>
        std::shared_future<T> Put(const Shared<TClass>& instance,ClassFunctionType<TClass,T> function);

        //template<typename T_ = T, std::enable_if_t<!std::is_void_v<T>>* = nullptr>
        //std::future<T> Put(const Shared<Delegate<T>>& task);

        void WaitForAll();
        void Stop();
    };

    template <typename T>
    template <typename T_, std::enable_if_t<std::is_void_v<T_>>*>
    void BackgroundThread<T>::RunTask(const std::shared_ptr<Task>& task)
    {
        try
        {
            task->fn->Invoke();
            task->promise.set_value();
        }
        catch (std::exception_ptr e)
        {
            task->promise.set_exception(e);
        }
    }

    template <typename T>
    template <typename T_, std::enable_if_t<!std::is_void_v<T_>>*>
    void BackgroundThread<T>::RunTask(const std::shared_ptr<Task>& task)
    {
        try
        {
            task->promise.set_value(task->fn->Invoke());
        }
        catch (std::exception e)
        {
            task->promise.set_exception(std::make_exception_ptr(e));
        }
    }
    

    template <typename T>
    BackgroundThread<T>::BackgroundThread(const std::string& name)
    {
        _name = name;
        _thread = std::thread([=]
        {
            while(true)
            {
                if(_pendingTasks.empty())
                {
                    std::unique_lock uLock(_lock);
                    _cond.wait(uLock);
                }

                auto task = _pendingTasks.front();
                if(!task)
                {
                    break;
                }

                _pendingTasks.pop();

                RunTask(task);
            }
        });
    }

    template <typename T>
    BackgroundThread<T>::~BackgroundThread()
    {
        Stop();
    }

    template <typename T>
    std::shared_future<T> BackgroundThread<T>::Put(const Shared<Delegate<T>>& task)
    {
        std::promise<T> prom{};
        std::shared_future<T> future(prom.get_future());
        auto newTask = std::make_shared<Task>(task,std::move(prom));
        if(_thread.joinable() && std::this_thread::get_id() == _thread.get_id())
        {
            RunTask(newTask);
            return future;
        }
        _pendingTasks.push(newTask);
        _cond.notify_one();
        return future;
    }

    template <typename T>
    std::shared_future<T> BackgroundThread<T>::Put(const std::function<T()>& func)
    {
        return Put(newDelegate(func));
    }

    template <typename T>
    std::shared_future<T> BackgroundThread<T>::Put(std::function<T()>&& func)
    {
        return Put(newDelegate(func));
    }

    template <typename T>
    template <typename TClass>
    std::shared_future<T> BackgroundThread<T>::Put(TClass* instance, ClassFunctionType<TClass, T> function)
    {
        return Put(newDelegate(instance,function));
    }

    template <typename T>
    template <typename TClass>
    std::shared_future<T> BackgroundThread<T>::Put(const Shared<TClass>& instance, ClassFunctionType<TClass, T> function)
    {
        return Put(newDelegate(instance,function));
    }

    template <typename T>
    void BackgroundThread<T>::WaitForAll()
    {
        if(_pendingTasks.empty()) return;
        _pendingTasks.back()->promise.get_future().wait();
    }

    template <typename T>
    void BackgroundThread<T>::Stop()
    {
        if(_thread.joinable())
        {
            _pendingTasks = {};
            _pendingTasks.push({});
            _cond.notify_one();
            _thread.join();
        }
    }
}
