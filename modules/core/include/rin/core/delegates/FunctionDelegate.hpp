#pragma once
#include <functional>
#include "Delegate.hpp"
#include "rin/core/memory.hpp"

template <typename TReturn, typename... TArgs>
class FunctionDelegate : public Delegate<TReturn, TArgs...>
{
public:
    explicit FunctionDelegate(const std::function<TReturn(TArgs...)>& func);
    TReturn Invoke(TArgs... args) override;
    [[nodiscard]] bool IsValid() const override;

private:
    using FunctionType = std::function<TReturn(TArgs...)>;

protected:
    FunctionType _function;
};

template <typename TReturn, typename... TArgs>
FunctionDelegate<TReturn, TArgs...>::FunctionDelegate(const std::function<TReturn(TArgs...)>& func)
{
    _function = func;
}

template <typename TReturn, typename... TArgs>
TReturn FunctionDelegate<TReturn, TArgs...>::Invoke(TArgs... args)
{
    return _function(std::forward<TArgs>(args)...);
}

template <typename TReturn, typename... TArgs>
bool FunctionDelegate<TReturn, TArgs...>::IsValid() const
{
    return true;
}


template <typename TReturn, typename... TArgs>
Shared<FunctionDelegate<TReturn, TArgs...>> newDelegate(const std::function<TReturn(TArgs...)>& func)
{
    return std::make_shared<FunctionDelegate<TReturn, TArgs...>>(func);
}

template <typename TReturn, typename... TArgs>
Shared<FunctionDelegate<TReturn, TArgs...>> newDelegate(TReturn (func)(TArgs...))
{
    return newFunctionDelegate(std::function(func));
}

template <typename TReturn, typename... TArgs>
Shared<FunctionDelegate<TReturn, TArgs...>> newDelegate(std::function<TReturn(TArgs...)>&& func)
{
    return std::make_shared<FunctionDelegate<TReturn, TArgs...>>(func);
}
