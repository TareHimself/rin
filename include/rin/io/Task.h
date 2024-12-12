#pragma once
#include <mutex>

#include <future>
#include <optional>
#include <vulkan/vulkan.hpp>
#include "rin/core/memory.h"
#include "rin/core/delegates/DelegateList.h"
#include "rin/core/math/Vec2.h"

namespace rin::io
{
    class TaskRunner;
    
    class Task
    {
        std::optional<std::exception_ptr> _exception{};
        std::mutex _mutex{};
        std::vector<Shared<Delegate<void,std::exception_ptr>>> _exceptionHandlers{};
        friend TaskRunner;
    protected:
        virtual void Run() = 0;
        
        void SetException(const std::exception_ptr& exception);

        std::optional<std::exception_ptr> GetException() const;

        std::mutex& GetMutex();

        std::vector<Shared<Delegate<void,std::exception_ptr>>>& GetExceptionHandlers();

    public:
        virtual ~Task();
        void OnException(const Shared<Delegate<void,std::exception_ptr>>& delegate);
        
        void OnException(const std::function<void(std::exception_ptr)>& func);

        void OnException(std::function<void(std::exception_ptr)>&& func);

        template <typename TClass>
        void OnException(TClass* instance, ClassFunctionType<TClass, std::exception_ptr> function);

        template <typename TClass>
        void OnException(const Shared<TClass>& instance, ClassFunctionType<TClass, std::exception_ptr> function);
    };

    inline void Task::SetException(const std::exception_ptr& exception)
    {
        _exception = exception;
    }

    inline std::optional<std::exception_ptr> Task::GetException() const
    {
        return _exception;
    }

    inline std::mutex& Task::GetMutex()
    {
        return _mutex;
    }

    inline std::vector<Shared<Delegate<void, std::exception_ptr>>>& Task::GetExceptionHandlers()
    {
        return _exceptionHandlers;
    }

    inline Task::~Task() = default;

    inline void Task::OnException(const std::function<void(std::exception_ptr)>& func)
    {
        OnException(newDelegate(func));
    }

    inline void Task::OnException(std::function<void(std::exception_ptr)>&& func)
    {
        OnException(newDelegate(func));
    }

    template <typename TClass>
    void Task::OnException(TClass* instance, ClassFunctionType<TClass, std::exception_ptr> function)
    {
        OnException(newDelegate(instance,function));
    }

    template <typename TClass>
    void Task::OnException(const Shared<TClass>& instance, ClassFunctionType<TClass, std::exception_ptr> function)
    {
        OnException(newDelegate(instance,function));
    }

    inline void Task::OnException(const Shared<Delegate<void, std::exception_ptr>>& delegate)
    {
        {
            std::unique_lock m(_mutex);
            _exceptionHandlers.push_back(delegate);
            if(_exception)
            {
                auto &exception = _exception.value();
                delegate->Invoke(exception);
            }
        }
    }

    class TaskNoResult : public Task
    {
        bool _completed = false;
        std::vector<Shared<Delegate<void>>> _completedHandlers{};
        Shared<Delegate<void>> _toRun{};
        
        void Run() override;

    public:
        explicit TaskNoResult(const Shared<Delegate<void>>& task);
        
        void OnCompleted(const Shared<Delegate<void>>& delegate);
        void OnCompleted(const std::function<void()>& func);
        void OnCompleted(std::function<void()>&& func);
        template<typename TClass>
        void OnCompleted(TClass* instance, ClassFunctionType<TClass,void> function);
        template<typename TClass>
        void OnCompleted(const Shared<TClass>& instance, ClassFunctionType<TClass,void> function);
    };

    inline void TaskNoResult::Run()
    {
        try
        {
            _toRun->Invoke();
            {
                std::unique_lock m(GetMutex());
                _completed = true;
                for (auto &completedHandler : _completedHandlers)
                {
                    completedHandler->Invoke();
                }
            }
        }
        catch (...)
        {
            {
                std::unique_lock m(GetMutex());
                SetException(std::current_exception());
                for (auto &handler : GetExceptionHandlers())
                {
                    handler->Invoke(*GetException());
                }
            }
        }
    }

    inline TaskNoResult::TaskNoResult(const Shared<Delegate<void>>& task)
    {
        _toRun = task;
        _completed = false;
    }
    
    inline void TaskNoResult::OnCompleted(const Shared<Delegate<void>>& delegate)
    {
        std::lock_guard m(this->GetMutex());
        _completedHandlers.push_back(delegate);
        if(_completed)
        {
            delegate->Invoke();
        }
    }

    inline void TaskNoResult::OnCompleted(const std::function<void()>& func)
    {
        OnCompleted(newDelegate(func));
    }

    inline void TaskNoResult::OnCompleted(std::function<void()>&& func)
    {
        OnCompleted(newDelegate(func));
    }

    template <typename TClass>
    void TaskNoResult::OnCompleted(TClass* instance, ClassFunctionType<TClass, void> function)
    {
        OnCompleted(newDelegate(instance,function));
    }

    template <typename TClass>
    void TaskNoResult::OnCompleted(const Shared<TClass>& instance, ClassFunctionType<TClass, void> function)
    {
        OnCompleted(newDelegate(instance,function));
    }

    template<typename T>
    class TaskWithResult : public Task
    {
        std::vector<Shared<Delegate<void,const T&>>> _completedHandlers{};
        
        std::optional<T> _result;
        std::optional<std::exception_ptr> _exception;
        std::condition_variable _cond{};
        Shared<Delegate<T>> _task{};
    public:
        explicit TaskWithResult(const Shared<Delegate<T>>& task);
        void OnCompleted(const Shared<Delegate<void,const T&>>& delegate);
        void OnCompleted(const std::function<void(const T&)>& func);
        void OnCompleted(std::function<void(const T&)>&& func);
        template<typename TClass>
        void OnCompleted(TClass* instance, ClassFunctionType<TClass,void,const T&> function);
        template<typename TClass>
        void OnCompleted(const Shared<TClass>& instance, ClassFunctionType<TClass,void,const T&> function);
    protected:
        
        
        void Run() override;
    };


    

    template <typename T>
    TaskWithResult<T>::TaskWithResult(const Shared<Delegate<T>>& task)
    {
        _task = task;
    }

    template <typename T>
    void TaskWithResult<T>::OnCompleted(const Shared<Delegate<void, const T&>>& delegate)
    {
        std::lock_guard m(this->GetMutex());
        _completedHandlers.push_back(delegate);
        if(_result)
        {
            auto &result = _result.value();
            delegate->Invoke(result);
        }
    }

    template <typename T>
    void TaskWithResult<T>::OnCompleted(const std::function<void(const T&)>& func)
    {
        OnCompleted(newDelegate(func));
    }

    template <typename T>
    void TaskWithResult<T>::OnCompleted(std::function<void(const T&)>&& func)
    {
        OnCompleted(newDelegate(func));
    }

    template <typename T>
    template <typename TClass>
    void TaskWithResult<T>::OnCompleted(TClass* instance, ClassFunctionType<TClass, void,const T&> function)
    {
        OnCompleted(newDelegate(instance,function));
    }

    template <typename T>
    template <typename TClass>
    void TaskWithResult<T>::OnCompleted(const Shared<TClass>& instance, ClassFunctionType<TClass, void,const T&> function)
    {
        OnCompleted(newDelegate(instance,function));
    }

    template <typename T>
    void TaskWithResult<T>::Run()
    {
        try
        {
            _result = _task->Invoke();
            {
                std::lock_guard m(this->GetMutex());
                for (auto &completedHandler : _completedHandlers)
                {
                    auto &result = _result.value();
                    completedHandler->Invoke(result);
                }
            }
        }
        catch (...)
        {
            {
                std::lock_guard m(this->GetMutex());
                this->SetException(std::current_exception());
                for (auto &handler : this->GetExceptionHandlers())
                {
                    handler->Invoke(*this->GetException());
                }
            }
        }
    }
}
