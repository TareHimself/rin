#pragma once
#include <functional>
#include <mutex>
namespace rin
{
    template<typename T>
    class Optional
    {
        union storage
        {
            T data;
            char empty;
        };
        storage _storage{};
        bool _empty{};
    public:
        template<std::enable_if_t<std::is_default_constructible_v<T>>* = nullptr>
        T ValueOrDefault();
        
        T ValueOr(const T& data);

        bool HasValue() const;

        void Clear();

        Optional& operator=(const T& other);
    };
    template <typename T>
    template <std::enable_if_t<std::is_default_constructible_v<T>>*>
    T Optional<T>::ValueOrDefault()
    {
        if(_empty)
        {
            return {};
        }

        return _storage.data;
    }
    template <typename T>
    T Optional<T>::ValueOr(const T& data)
    {
        if(_empty)
        {
            return {};
        }

        return _storage.data;
    }
    template <typename T>
    bool Optional<T>::HasValue() const
    {
        return _empty;
    }
    template <typename T>
    void Optional<T>::Clear()
    {
        if(!_empty)
        {
            _storage.empty = {};
            _empty = true;
        }
    }
    template <typename T>
    Optional<T>& Optional<T>::operator=(const T& other)
    {
        _empty = false;
        _storage.data = other;
        return *this;
    }

}
