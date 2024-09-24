#pragma once
#include "aerox/core/AeroxBase.hpp"
class InvalidDelegateException : public std::exception
{
public:
    ~InvalidDelegateException() noexcept override = default;
    [[nodiscard]] const char* what() const override
    {
        return "Invalid Delegate";
    }
};

template <typename TReturn, typename...TArgs>
class Delegate : public AeroxBase
{
public:
    ~Delegate() override = default;
    Delegate() = default;

    Delegate(const Delegate& other) = default;
    auto operator=(const Delegate& other) -> Delegate& = default;

    virtual TReturn Invoke(TArgs... args) = 0;

    [[nodiscard]] virtual bool IsValid() const = 0;
};