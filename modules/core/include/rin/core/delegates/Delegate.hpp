#pragma once
#include "rin/core/RinBase.hpp"

class InvalidDelegateException : public std::exception
{
public:
    ~InvalidDelegateException() noexcept override = default;

    [[nodiscard]] const char* what() const override
    {
        return "Invalid Delegate";
    }
};

template <typename TReturn, typename... TArgs>
class Delegate : public RinBase
{
public:
    ~Delegate() override = default;
    Delegate() = default;

    Delegate(const Delegate& other) = default;
    Delegate& operator=(const Delegate& other) = default;

    virtual TReturn Invoke(TArgs... args) = 0;

    [[nodiscard]] virtual bool IsValid() const = 0;
};
