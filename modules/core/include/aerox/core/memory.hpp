#pragma once
#include <memory>
#include "aerox/core/Disposable.hpp"
namespace aerox
{

    template<typename T,typename ...Args>
    using SharedEnable = std::enable_if_t<std::is_constructible_v<T,Args...> && std::is_base_of_v<Disposable,T>,Shared<T>>;

    template<typename T,typename ...Args>
    using WeakEnable = std::enable_if_t<std::is_constructible_v<T,Args...> && std::is_base_of_v<Disposable,T>,Weak<T>>;

    template<typename T,typename ...Args>
    using UniqueEnable = std::enable_if_t<std::is_constructible_v<T,Args...> && std::is_base_of_v<Disposable,T>,Unique<T>>;

    template <typename T, typename... TArgs>
    SharedEnable<T,TArgs...> newShared(TArgs&&... args) {
        return std::shared_ptr<T>(new T(std::forward<TArgs>(args)...),[](T * ptr)
        {
            ptr->Dispose(false);
            delete ptr;
        });
    }

    template <typename T, typename... TArgs>
    UniqueEnable<T,TArgs...> newUnique(TArgs&&... args) {
        return std::unique_ptr<T>(new T(std::forward<TArgs>(args)...),[](T * ptr)
        {
            ptr->Dispose(false);
            delete ptr;
        });
    }
}


