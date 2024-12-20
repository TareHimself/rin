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
    
    class __Task
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
        virtual ~__Task();
        void OnException(const Shared<Delegate<void,std::exception_ptr>>& delegate);
        
        void OnException(const std::function<void(std::exception_ptr)>& func);

        void OnException(std::function<void(std::exception_ptr)>&& func);

        template <typename TClass>
        void OnException(TClass* instance, ClassFunctionType<TClass, std::exception_ptr> function);

        template <typename TClass>
        void OnException(const Shared<TClass>& instance, ClassFunctionType<TClass, std::exception_ptr> function);

        
        virtual bool HasResult() = 0;
        virtual bool HasException();
    };

    inline void __Task::SetException(const std::exception_ptr& exception)
    {
        _exception = exception;
    }

    inline std::optional<std::exception_ptr> __Task::GetException() const
    {
        return _exception;
    }

    inline std::mutex& __Task::GetMutex()
    {
        return _mutex;
    }

    inline std::vector<Shared<Delegate<void, std::exception_ptr>>>& __Task::GetExceptionHandlers()
    {
        return _exceptionHandlers;
    }

    inline __Task::~__Task() = default;

    inline void __Task::OnException(const std::function<void(std::exception_ptr)>& func)
    {
        OnException(newDelegate(func));
    }

    inline void __Task::OnException(std::function<void(std::exception_ptr)>&& func)
    {
        OnException(newDelegate(func));
    }

    inline bool __Task::HasException()
    {
        return _exception.has_value();
    }

    template <typename TClass>
    void __Task::OnException(TClass* instance, ClassFunctionType<TClass, std::exception_ptr> function)
    {
        OnException(newDelegate(instance,function));
    }

    template <typename TClass>
    void __Task::OnException(const Shared<TClass>& instance, ClassFunctionType<TClass, std::exception_ptr> function)
    {
        OnException(newDelegate(instance,function));
    }

    inline void __Task::OnException(const Shared<Delegate<void, std::exception_ptr>>& delegate)
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

    class __TaskNoResult : public __Task
    {
        bool _completed = false;
        std::promise<void> _promise{};
        std::shared_future<void> _pending;
        std::vector<Shared<Delegate<void>>> _completedHandlers{};
        Shared<Delegate<void>> _toRun{};
        
        void Run() override;

    public:
        explicit __TaskNoResult(const Shared<Delegate<void>>& task);
        
        void OnCompleted(const Shared<Delegate<void>>& delegate);
        void OnCompleted(const std::function<void()>& func);
        void OnCompleted(std::function<void()>&& func);
        template<typename TClass>
        void OnCompleted(TClass* instance, ClassFunctionType<TClass,void> function);
        template<typename TClass>
        void OnCompleted(const Shared<TClass>& instance, ClassFunctionType<TClass,void> function);
        std::shared_future<void> GetFuture() const;

        bool HasResult() override;
    };

    inline void __TaskNoResult::Run()
    {
        try
        {
            _toRun->Invoke();
            {
                std::unique_lock m(GetMutex());
                _completed = true;
                _promise.set_value();
                for (auto &completedHandler : _completedHandlers)
                {
                    try
                    {
                        completedHandler->Invoke();
                    }
                    catch (...)
                    {
                    }
                }
            }
        }
        catch (...)
        {
            {
                std::unique_lock m(GetMutex());
                SetException(std::current_exception());
                _promise.set_exception(this->GetException().value());
                for (auto &handler : GetExceptionHandlers())
                {
                    try
                    {
                        handler->Invoke(*GetException());
                    }
                    catch (...)
                    {
                    }
                }
            }
        }
    }

    inline __TaskNoResult::__TaskNoResult(const Shared<Delegate<void>>& task)
    {
        _toRun = task;
        _completed = false;
        _pending = _promise.get_future();
    }
    
    inline void __TaskNoResult::OnCompleted(const Shared<Delegate<void>>& delegate)
    {
        std::lock_guard m(this->GetMutex());
        _completedHandlers.push_back(delegate);
        if(_completed)
        {
            try
            {
                delegate->Invoke();
            }
            catch (...)
            {
            }
        }
    }

    inline void __TaskNoResult::OnCompleted(const std::function<void()>& func)
    {
        OnCompleted(newDelegate(func));
    }

    inline void __TaskNoResult::OnCompleted(std::function<void()>&& func)
    {
        OnCompleted(newDelegate(func));
    }

    inline std::shared_future<void> __TaskNoResult::GetFuture() const
    {
        return _pending;
    }

    inline bool __TaskNoResult::HasResult()
    {
        return _completed;
    }

    template <typename TClass>
    void __TaskNoResult::OnCompleted(TClass* instance, ClassFunctionType<TClass, void> function)
    {
        OnCompleted(newDelegate(instance,function));
    }

    template <typename TClass>
    void __TaskNoResult::OnCompleted(const Shared<TClass>& instance, ClassFunctionType<TClass, void> function)
    {
        OnCompleted(newDelegate(instance,function));
    }

    template<typename T>
    class __TaskWithResult : public __Task
    {
        std::vector<Shared<Delegate<void,const T&>>> _completedHandlers{};
        std::promise<T> _promise{};
        std::shared_future<T> _future;
        std::optional<T> _result;
        std::optional<std::exception_ptr> _exception;
        std::condition_variable _cond{};
        Shared<Delegate<T>> _task{};
    public:
        explicit __TaskWithResult(const Shared<Delegate<T>>& task);
        void OnCompleted(const Shared<Delegate<void,const T&>>& delegate);
        void OnCompleted(const std::function<void(const T&)>& func);
        void OnCompleted(std::function<void(const T&)>&& func);
        template<typename TClass>
        void OnCompleted(TClass* instance, ClassFunctionType<TClass,void,const T&> function);
        template<typename TClass>
        void OnCompleted(const Shared<TClass>& instance, ClassFunctionType<TClass,void,const T&> function);

        void CallCompleted();

        std::shared_future<T> GetFuture() const;

        bool HasResult() override;
    protected:
        
        
        void Run() override;
    };


    

    template <typename T>
    __TaskWithResult<T>::__TaskWithResult(const Shared<Delegate<T>>& task)
    {
        _task = task;
        _future = _promise.get_future();
    }

    template <typename T>
    void __TaskWithResult<T>::OnCompleted(const Shared<Delegate<void, const T&>>& delegate)
    {
        std::lock_guard m(this->GetMutex());
        _completedHandlers.push_back(delegate);
        if(_result)
        {
            auto &result = _result.value();
            try
            {
                delegate->Invoke(result);
            }
            catch (...)
            {
            }
        }
    }

    template <typename T>
    void __TaskWithResult<T>::OnCompleted(const std::function<void(const T&)>& func)
    {
        OnCompleted(newDelegate(func));
    }

    template <typename T>
    void __TaskWithResult<T>::OnCompleted(std::function<void(const T&)>&& func)
    {
        OnCompleted(newDelegate(func));
    }

    template <typename T>
    template <typename TClass>
    void __TaskWithResult<T>::OnCompleted(TClass* instance, ClassFunctionType<TClass, void,const T&> function)
    {
        OnCompleted(newDelegate(instance,function));
    }

    template <typename T>
    template <typename TClass>
    void __TaskWithResult<T>::OnCompleted(const Shared<TClass>& instance, ClassFunctionType<TClass, void,const T&> function)
    {
        OnCompleted(newDelegate(instance,function));
    }
    

    template <typename T>
    std::shared_future<T> __TaskWithResult<T>::GetFuture() const
    {
        return _future;
    }

    template <typename T>
    bool __TaskWithResult<T>::HasResult()
    {
        return _result.has_value();
    }

    template <typename T>
    void __TaskWithResult<T>::Run()
    {
        try
        {
            _result = _task->Invoke();
            {
                std::lock_guard m(this->GetMutex());
                _promise.set_value(_result.value());
                for (auto &completedHandler : _completedHandlers)
                {
                    auto &result = _result.value();
                    try
                    {
                        completedHandler->Invoke(result);
                    }
                    catch (...)
                    {
                    }
                }
            }
        }
        catch (...)
        {
            {
                
                std::lock_guard m(this->GetMutex());
                auto exception = std::current_exception();
                this->SetException(exception);
                _promise.set_exception(exception);
                for (auto &handler : this->GetExceptionHandlers())
                {
                    try
                    {
                        handler->Invoke(*this->GetException());
                    }
                    catch (...)
                    {
                        
                    }
                }
            }
        }
    }


    // Ease of use typedef
    template<typename T = void>
    using Task = std::conditional_t<std::is_void_v<T>, Shared<__TaskNoResult>, Shared<__TaskWithResult<T>>>;
    
}
