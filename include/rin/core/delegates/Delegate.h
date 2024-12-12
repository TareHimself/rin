#pragma once
#include <exception>

namespace rin
{
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
    class Delegate
    {
    public:
        virtual ~Delegate() = default;
        Delegate() = default;

        Delegate(const Delegate& other) = default;
        Delegate& operator=(const Delegate& other) = default;

        virtual TReturn Invoke(TArgs... args) = 0;

        [[nodiscard]] virtual bool IsValid() const = 0;
    };
    
}
