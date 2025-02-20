#pragma once
#include <type_traits>
#include <memory>
#include "Disposable.h"

namespace rin
{
    template<typename T>
    using Shared = std::shared_ptr<T>;

    template<typename T>
    using Weak = std::weak_ptr<T>;
    
    template<typename T,typename ...TArgs>
    std::enable_if_t<std::is_base_of_v<Disposable,T> &&  std::is_constructible_v<T,TArgs...>,Shared<T>> shared(TArgs&&... args)
    {
        auto ptr = new T(std::forward<TArgs>(args)...);
        
        return std::shared_ptr<T>(ptr,[](T * ptr)
        {
            static_cast<Disposable*>(ptr)->Dispose();
        });
    }

    template<typename T,typename ...TArgs>
    std::enable_if_t<!std::is_base_of_v<Disposable,T> && std::is_constructible_v<T,TArgs...>,Shared<T>> shared(TArgs&&... args)
    {
        return std::make_shared<T>(std::forward<TArgs>(args)...);
    }

    // template<typename T,typename ...TArgs>
    // std::enable_if_t<std::is_constructible_v<T,TArgs...>,Shared<T>> shared(TArgs&&... args)
    // {
    //     return std::make_shared<T>(std::forward<TArgs>(args)...);
    // }

    template<typename E,typename T>
Shared<E> dynamicCast(const Shared<T>& target)
    {
        return std::dynamic_pointer_cast<E>(target);
    }

    template<typename E,typename T>
Shared<E> staticCast(const Shared<T>& target)
    {
        return std::static_pointer_cast<E>(target);
    }
}
