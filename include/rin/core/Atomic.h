#pragma once
#include <functional>
#include <mutex>
namespace rin
{
    template<typename T>
    class Atomic
    {
        std::mutex _mutex;
        T _data;
    public:
        template<std::enable_if_t<std::is_default_constructible_v<T>>* = nullptr>
        Atomic();

        Atomic(const T& data);

        Atomic& Update(const std::function<void(T&)>& update);

        Atomic& operator=(const T& other);

        explicit operator T() const;
    };
    
    template <typename T>
    template <std::enable_if_t<std::is_default_constructible_v<T>>*>
    Atomic<T>::Atomic()
    {
        _data = {};
    }
    
    template <typename T>
    Atomic<T>::Atomic(const T& data)
    {
        _data = data;
    }
    template <typename T>
    Atomic<T>& Atomic<T>::Update(const std::function<void(T&)>& update)
    {
        std::lock_guard lock(_mutex);
        update(_data);
        return *this;
    }
    template <typename T>
    Atomic<T>& Atomic<T>::operator=(const T& other)
    {
        std::lock_guard lock(_mutex);
        _data = other;
        return *this;
    }
    template <typename T>
    Atomic<T>::operator T() const
    {
        std::lock_guard lock(_mutex);
        return _data;
    }
}
