#pragma once
#include <utility>

#include "Delegate.hpp"
#include "rin/core/memory.hpp"
template <class TClass, typename TReturn, typename... TArgs>
using ClassFunctionType = TReturn(TClass::*)(TArgs...);

template <typename TClass, typename TReturn, typename... TArgs>
class ClassFunctionDelegate : public Delegate<TReturn, TArgs...>
{
    TClass* _instance = nullptr;
    ClassFunctionType<TClass, TReturn, TArgs...> _function = nullptr;

public:
    explicit ClassFunctionDelegate(TClass* instance, ClassFunctionType<TClass, TReturn, TArgs...> function);

    TReturn Invoke(TArgs... args) override;

    [[nodiscard]] bool IsValid() const override;
};

template <typename TClass, typename TReturn, typename... TArgs>
ClassFunctionDelegate<TClass, TReturn, TArgs...>::ClassFunctionDelegate(TClass* instance,
                                                                        ClassFunctionType<TClass, TReturn, TArgs...>
                                                                        function)
{
    _instance = instance;
    _function = function;
}

template <typename TClass, typename TReturn, typename... TArgs>
TReturn ClassFunctionDelegate<TClass, TReturn, TArgs...>::Invoke(TArgs... args)
{
    return (_instance->*_function)(std::forward<TArgs>(args)...);
}

template <typename TClass, typename TReturn, typename... TArgs>
bool ClassFunctionDelegate<TClass, TReturn, TArgs...>::IsValid() const
{
    return _instance != nullptr;
}


template <typename TClass, typename TReturn, typename... TArgs>
Shared<ClassFunctionDelegate<TClass, TReturn, TArgs...>> newDelegate(TClass* instance,
                                                                     ClassFunctionType<TClass, TReturn, TArgs...>
                                                                     function)
{
    return std::make_shared<ClassFunctionDelegate<TClass, TReturn, TArgs...>>(instance, function);
}
