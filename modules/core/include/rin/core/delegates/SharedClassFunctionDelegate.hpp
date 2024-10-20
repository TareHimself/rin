#pragma once
#include <utility>
#include "Delegate.hpp"
#include "rin/core/memory.hpp"
template <class TClass, typename TReturn, typename... TArgs>
using ClassFunctionType = TReturn(TClass::*)(TArgs...);

template <typename TClass, typename TReturn, typename... TArgs>
class SharedClassFunctionDelegate : public Delegate<TReturn, TArgs...>
{
    Weak<TClass> _instance{};
    ClassFunctionType<TClass, TReturn, TArgs...> _function = nullptr;
    [[nodiscard]] bool IsValid() const override;

public:
    explicit SharedClassFunctionDelegate(const Shared<TClass>& instance,
                                         ClassFunctionType<TClass, TReturn, TArgs...> function);

    TReturn Invoke(TArgs... args) override;
};

template <typename TClass, typename TReturn, typename... TArgs>
bool SharedClassFunctionDelegate<TClass, TReturn, TArgs...>::IsValid() const
{
    if (auto shared = _instance.lock())
    {
        return true;
    }
    return false;
}

template <typename TClass, typename TReturn, typename... TArgs>
SharedClassFunctionDelegate<TClass, TReturn, TArgs...>::SharedClassFunctionDelegate(const Shared<TClass>& instance,
    ClassFunctionType<TClass, TReturn, TArgs...> function)
{
    _instance = instance;
    _function = function;
}

template <typename TClass, typename TReturn, typename... TArgs>
TReturn SharedClassFunctionDelegate<TClass, TReturn, TArgs...>::Invoke(TArgs... args)
{
    if (auto shared = _instance.lock())
    {
        auto ptr = shared.get();
        return (ptr->*_function)(std::forward<TArgs>(args)...);
    }

    throw InvalidDelegateException();
}


template <typename TClass, typename TReturn, typename... TArgs>
Shared<SharedClassFunctionDelegate<TClass, TReturn, TArgs...>> newDelegate(
    const Shared<TClass>& instance, ClassFunctionType<TClass, TReturn, TArgs...> function)
{
    return std::make_shared<SharedClassFunctionDelegate<TClass, TReturn, TArgs...>>(instance, function);
}
