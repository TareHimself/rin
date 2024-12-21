#pragma once
#include <mutex>
#include <future>
#include <optional>
#include "rin/core/memory.h"
#include "rin/core/delegates/DelegateList.h"
#include "rin/core/math/Vec2.h"

namespace rin::io
{
    class TaskRunner;

    // Base class for tasks
    class BaseTask
    {
        std::optional<std::exception_ptr> _exception{};
        std::mutex _mutex{};
        std::vector<Shared<Delegate<void, std::exception_ptr>>> _exceptionHandlers{};

    protected:
        virtual void SetException(const std::exception_ptr& exception);

        std::optional<std::exception_ptr> GetException() const;

        std::mutex& GetMutex();

        std::vector<Shared<Delegate<void, std::exception_ptr>>>& GetExceptionHandlers();

    public:
        virtual ~BaseTask();
        void OnException(const Shared<Delegate<void, std::exception_ptr>>& delegate);

        void OnException(const std::function<void(std::exception_ptr)>& func);

        void OnException(std::function<void(std::exception_ptr)>&& func);

        template <typename TClass>
        void OnException(TClass* instance, ClassFunctionType<TClass, std::exception_ptr> function);

        template <typename TClass>
        void OnException(const Shared<TClass>& instance, ClassFunctionType<TClass, std::exception_ptr> function);


        virtual bool HasResult() = 0;
        virtual bool HasException();
    };

    inline void BaseTask::SetException(const std::exception_ptr& exception)
    {
        _exception = exception;
        {
            std::unique_lock m(_mutex);
            for (auto& exceptionHandler : _exceptionHandlers)
            {
                exceptionHandler->Invoke(_exception.value());
            }
        }
    }

    inline std::optional<std::exception_ptr> BaseTask::GetException() const
    {
        return _exception;
    }

    inline std::mutex& BaseTask::GetMutex()
    {
        return _mutex;
    }

    inline std::vector<Shared<Delegate<void, std::exception_ptr>>>& BaseTask::GetExceptionHandlers()
    {
        return _exceptionHandlers;
    }

    inline BaseTask::~BaseTask() = default;

    inline void BaseTask::OnException(const std::function<void(std::exception_ptr)>& func)
    {
        OnException(newDelegate(func));
    }

    inline void BaseTask::OnException(std::function<void(std::exception_ptr)>&& func)
    {
        OnException(newDelegate(func));
    }

    inline bool BaseTask::HasException()
    {
        return _exception.has_value();
    }

    template <typename TClass>
    void BaseTask::OnException(TClass* instance, ClassFunctionType<TClass, std::exception_ptr> function)
    {
        OnException(newDelegate(instance, function));
    }

    template <typename TClass>
    void BaseTask::OnException(const Shared<TClass>& instance, ClassFunctionType<TClass, std::exception_ptr> function)
    {
        OnException(newDelegate(instance, function));
    }

    inline void BaseTask::OnException(const Shared<Delegate<void, std::exception_ptr>>& delegate)
    {
        {
            std::unique_lock m(_mutex);
            _exceptionHandlers.push_back(delegate);
            if (_exception)
            {
                auto& exception = _exception.value();
                delegate->Invoke(exception);
            }
        }
    }

    template <typename T>
    class TTask;
    template <typename T>
    class TContinuation;
    

    template <typename T>
    class TTask : public BaseTask
    {
        std::promise<T> _promise{};
        std::optional<T> _result{};
        std::vector<Shared<Delegate<void, const T&>>> _completedHandlers{};

    public:
        void SetException(const std::exception_ptr& exception) override;
        void SetResult(const T& result);

        void OnCompleted(const Shared<Delegate<void, const T&>>& delegate);
        void OnCompleted(const std::function<void(const T&)>& func);
        void OnCompleted(std::function<void(const T&)>&& func);
        template <typename TClass>
        void OnCompleted(TClass* instance, ClassFunctionType<TClass, void, const T&> function);
        template <typename TClass>
        void OnCompleted(const Shared<TClass>& instance, ClassFunctionType<TClass, void, const T&> function);
        bool HasResult() override;
        T Wait();

        // template<typename E>
        // Shared<TContinuation<E>> Then(const Shared<Delegate<E,T>>& delegate);
    };

    template <>
    class TTask<void> : public BaseTask
    {
        std::promise<void> _promise{};
        std::vector<Shared<Delegate<void>>> _completedHandlers{};
        bool _completed = false;

    public:
        void SetException(const std::exception_ptr& exception) override;
        void SetResult();

        void OnCompleted(const Shared<Delegate<void>>& delegate);
        void OnCompleted(const std::function<void()>& func);
        void OnCompleted(std::function<void()>&& func);
        template <typename TClass>
        void OnCompleted(TClass* instance, ClassFunctionType<TClass, void> function);
        template <typename TClass>
        void OnCompleted(const Shared<TClass>& instance, ClassFunctionType<TClass, void> function);
        bool HasResult() override;
        void Wait();
    };
    
    // template <typename T>
    // class TContinuation : public TTask<T>
    // {
    // public:
    //     template<typename To = T,typename From, std::enable_if_t<std::is_void_v<_T>>* = nullptr>
    //     TContinuation(TTask<T> * source,const Shared<Delegate<T>>& continuation);
    //     template<typename To = T,typename From, std::enable_if_t<!std::is_void_v<_T>>* = nullptr>
    //     TContinuation(TTask<T> * source,const Shared<Delegate<T,>>& continuation);
    // };

    class Runnable
    {
    public:
        virtual ~Runnable() = default;

    private:
        friend TaskRunner;

    protected:
        virtual void Run() = 0;
    };

    template <typename T>
    class RunnableTask : public TTask<T>, public Runnable
    {
        Shared<Delegate<T>> _task{};

    protected:
        void Run() override;

        template <typename _T = T, std::enable_if_t<!std::is_void_v<_T>>* = nullptr>
        void HandleRun();

        template <typename _T = T, std::enable_if_t<std::is_void_v<_T>>* = nullptr>
        void HandleRun();

    public:
        explicit RunnableTask(const Shared<Delegate<T>>& task);
    };
    
    template <typename T>
    void TTask<T>::SetException(const std::exception_ptr& exception)
    {
        BaseTask::SetException(exception);
    }

    template <typename T>
    void TTask<T>::SetResult(const T& result)
    {
        std::lock_guard g(GetMutex());
        _promise.set_value(result);
        _result = result;
        for (auto& completedHandler : _completedHandlers)
        {
            try
            {
                completedHandler->Invoke(result);
            }
            catch (...)
            {
            }
        }
    }

    template <typename T>
    void TTask<T>::OnCompleted(const Shared<Delegate<void, const T&>>& delegate)
    {
        std::lock_guard g(GetMutex());
        _completedHandlers.emplace_back(delegate);
        if (_result.has_value())
        {
            auto r = _result.value();
            delegate->Invoke(r);
        }
    }

    template <typename T>
    void TTask<T>::OnCompleted(const std::function<void(const T&)>& func)
    {
        OnCompleted(newDelegate(func));
    }

    template <typename T>
    void TTask<T>::OnCompleted(std::function<void(const T&)>&& func)
    {
        OnCompleted(newDelegate(func));
    }

    template <typename T>
    template <typename TClass>
    void TTask<T>::OnCompleted(TClass* instance, ClassFunctionType<TClass, void, const T&> function)
    {
        OnCompleted(newDelegate(instance, function));
    }

    template <typename T>
    template <typename TClass>
    void TTask<T>::OnCompleted(const Shared<TClass>& instance, ClassFunctionType<TClass, void, const T&> function)
    {
        OnCompleted(newDelegate(instance, function));
    }

    template <typename T>
    bool TTask<T>::HasResult()
    {
        return _result.has_value();
    }

    template <typename T>
    T TTask<T>::Wait()
    {
        return _promise.get_future().get();
    }

    

    inline void TTask<void>::SetException(const std::exception_ptr& exception)
    {
        BaseTask::SetException(exception);
        _promise.set_exception(exception);
    }

    inline void TTask<void>::SetResult()
    {
        std::lock_guard g(GetMutex());
        _promise.set_value();
        _completed = true;
        for (auto& completedHandler : _completedHandlers)
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

    inline void TTask<void>::OnCompleted(const Shared<Delegate<void>>& delegate)
    {
        std::lock_guard g(GetMutex());
        _completedHandlers.emplace_back(delegate);
        if (_completed)
        {
            delegate->Invoke();
        }
    }

    inline void TTask<void>::OnCompleted(const std::function<void()>& func)
    {
        OnCompleted(newDelegate(func));
    }

    inline void TTask<void>::OnCompleted(std::function<void()>&& func)
    {
        OnCompleted(newDelegate(func));
    }

    inline bool TTask<void>::HasResult()
    {
        return _completed;
    }

    inline void TTask<void>::Wait()
    {
        _promise.get_future().wait();
    }

    template <typename TClass>
    void TTask<void>::OnCompleted(TClass* instance, ClassFunctionType<TClass, void> function)
    {
        OnCompleted(newDelegate(instance, function));
    }

    template <typename TClass>
    void TTask<void>::OnCompleted(const Shared<TClass>& instance, ClassFunctionType<TClass, void> function)
    {
        OnCompleted(newDelegate(instance, function));
    }

    // template <typename T>
    // template <typename _T, std::enable_if_t<std::is_void_v<_T>>*>
    // TContinuation<T>::TContinuation(TTask<T>* source)
    // {
    //     source->OnCompleted([this]
    //     {
    //         this->SetResult();
    //     });
    // }
    //
    // template <typename T>
    // template <typename _T, std::enable_if_t<!std::is_void_v<_T>>*>
    // TContinuation<T>::TContinuation(TTask<T>* source)
    // {
    //     source->OnCompleted([this](const T& )
    //    {
    //        this->SetResult();
    //    });
    // }

    template <typename T>
    void RunnableTask<T>::Run()
    {
        HandleRun();
    }

    template <typename T>
    template <typename _T, std::enable_if_t<!std::is_void_v<_T>>*>
    void RunnableTask<T>::HandleRun()
    {
        try
        {
            this->SetResult(_task->Invoke());
        }
        catch (...)
        {
            this->OnException(std::current_exception());
        }
    }

    template <typename T>
    template <typename _T, std::enable_if_t<std::is_void_v<_T>>*>
    void RunnableTask<T>::HandleRun()
    {
        try
        {
            _task->Invoke();
            this->SetResult();
        }
        catch (...)
        {
            this->SetException(std::current_exception());
        }
    }

    template <typename T>
    RunnableTask<T>::RunnableTask(const Shared<Delegate<T>>& task)
    {
        _task = task;
    }

    // Ease of use typedef
    template <typename T = void>
    using Task = Shared<TTask<T>>; //std::conditional_t<std::is_void_v<T>, Shared<RTask<void>>, Shared<RTask<T>>>;
}
