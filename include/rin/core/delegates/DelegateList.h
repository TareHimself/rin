#pragma once
#include <functional>
#include <map>

#include "ClassFunctionDelegate.h"
#include "SharedClassFunctionDelegate.h"
#include "Delegate.h"
#include "FunctionDelegate.h"
#include "rin/core/macros.h"
namespace rin
{
    class _DelegateList;
    template <typename... TArgs>
    class DelegateList;

    struct DelegateListHandle
    {
    private:
        uint64_t _id = 0;
        Weak<_DelegateList> _delegateList{};

    public:
        DelegateListHandle();
        DelegateListHandle(uint64_t id, const Weak<_DelegateList>& delegateList);

        [[nodiscard]] bool IsBound() const;

        bool UnBind();
    };

    class _DelegateList : public Disposable, public std::enable_shared_from_this<_DelegateList>
    {
    public:
        virtual bool IsBound(uint64_t id) const = 0;
        virtual bool UnBind(uint64_t id) = 0;
        virtual bool HasBindings() const = 0;
        virtual void Clear() = 0;
    };


    inline DelegateListHandle::DelegateListHandle() : DelegateListHandle(0, {})
    {
    }

    inline DelegateListHandle::DelegateListHandle(uint64_t id, const Weak<_DelegateList>& delegateList)
    {
        {
            _delegateList = delegateList;
            _id = id;
        }
    }

    inline bool DelegateListHandle::IsBound() const
    {
        if (auto list = _delegateList.lock())
        {
            return list->IsBound(_id);
        }

        return false;
    }

    inline bool DelegateListHandle::UnBind()
    {
        if (!IsBound())
        {
            return false;
        }

        if (const auto list = _delegateList.lock())
        {
            _delegateList = {};
            return list->UnBind(_id);
        }

        return false;
    }


    template <typename... TArgs>
    class DelegateList : public _DelegateList
    {
        std::map<uint64_t, Shared<Delegate<void, TArgs...>>> _delegates{};
        //std::map<uint64_t, std::function<void(TArgs...)>> _delegates{};
    public:
        DelegateList() = default;
        DelegateListHandle Add(const Shared<Delegate<void, TArgs...>>& delegate);
        //DelegateListHandle Add(TReturn (func)(TArgs...));
        DelegateListHandle Add(const std::function<void(TArgs...)>& func);
        DelegateListHandle Add(std::function<void(TArgs...)>&& func);
        template <typename TClass>
        DelegateListHandle Add(TClass* instance, ClassFunctionType<TClass, void, TArgs...> function);
        template <typename TClass>
        DelegateListHandle Add(const Shared<TClass>& instance, ClassFunctionType<TClass, void, TArgs...> function);

        void Invoke(TArgs... args);

        bool IsBound(uint64_t id) const override;

        bool UnBind(uint64_t id) override;

        bool HasBindings() const override;

        void Clear() override;

    protected:
        void OnDispose() override;
    };

    template <typename... TArgs>
    DelegateListHandle DelegateList<TArgs...>::Add(const Shared<Delegate<void, TArgs...>>& delegate)
    {
        if (_delegates.empty())
        {
            _delegates.emplace(0, delegate);
            return DelegateListHandle{0, this->weak_from_this()};
        }

        auto id = _delegates.rbegin()->first + 1;
        _delegates.emplace(id, delegate);
        return DelegateListHandle{id, this->weak_from_this()};
    }

    // template <typename ... TArgs>
    // DelegateListHandle TDelegateList<TReturn, TArgs...>::Add(TReturn func(TArgs...))
    // {
    //     return Add(newDelegate(func));
    // }

    template <typename... TArgs>
    DelegateListHandle DelegateList<TArgs...>::Add(const std::function<void(TArgs...)>& func)
    {
        return Add(newDelegate(func));
    }

    template <typename... TArgs>
    DelegateListHandle DelegateList<TArgs...>::Add(std::function<void(TArgs...)>&& func)
    {
        return Add(newDelegate(func));
    }

    template <typename... TArgs>
    template <typename TClass>
    DelegateListHandle DelegateList<TArgs...>::Add(TClass* instance,
                                                   ClassFunctionType<TClass, void, TArgs...> function)
    {
        return Add(newDelegate(instance, function));
    }

    template <typename... TArgs>
    template <typename TClass>
    DelegateListHandle DelegateList<TArgs...>::Add(const Shared<TClass>& instance,
                                                   ClassFunctionType<TClass, void, TArgs...> function)
    {
        return Add(newDelegate(instance, function));
    }

    template <typename... TArgs>
    void DelegateList<TArgs...>::Invoke(TArgs... args)
    {
        for (auto& [_,delegate] : _delegates)
        {
            delegate->Invoke(std::forward<TArgs>(args)...);
        }
    }

    template <typename... TArgs>
    bool DelegateList<TArgs...>::IsBound(uint64_t id) const
    {
        return _delegates.contains(id);
    }

    template <typename... TArgs>
    bool DelegateList<TArgs...>::UnBind(uint64_t id)
    {
        if (_delegates.contains(id))
        {
            _delegates.erase(_delegates.find(id));
            return true;
        }

        return false;
    }

    template <typename... TArgs>
    bool DelegateList<TArgs...>::HasBindings() const
    {
        return !_delegates.empty();
    }
    template <typename ... TArgs>
    void DelegateList<TArgs...>::Clear()
    {
        _delegates.clear();
    }

    template <typename ... TArgs>
    void DelegateList<TArgs...>::OnDispose()
    {
        _delegates.clear();
    }


    template <typename... TArgs>
    Shared<DelegateList<TArgs...>> newDelegateList()
    {
        return shared<DelegateList<TArgs...>>();
    }
}

#ifndef DEFINE_DELEGATE_LIST
#define DEFINE_DELEGATE_LIST(Name,...) \
rin::Shared<rin::DelegateList<__VA_ARGS__>> ##Name = rin::newDelegateList<__VA_ARGS__>();
#endif