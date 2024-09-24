#pragma once
#include <atomic>
#include <memory>

template<typename T>
    using Shared = std::shared_ptr<T>;

template<typename T>
using Weak = std::weak_ptr<T>;

template<typename T>
using Unique = std::unique_ptr<T>;
    
    
    
class Disposable : public std::enable_shared_from_this<Disposable>
{
        
    std::atomic<bool> _disposed = false;
protected:
    virtual void OnDispose(bool manual);
public:
    virtual ~Disposable() = default;
    void Dispose(bool manual = true);
    bool IsDisposed() const;


    template<typename T,typename = std::enable_if<std::is_base_of_v<Disposable,T>,T>>
    Shared<T> GetSharedDynamic();

    template<typename T,typename = std::enable_if<std::is_base_of_v<Disposable,T>,T>>
    Weak<T> GetWeakDynamic();

    template<typename T,typename = std::enable_if<std::is_base_of_v<Disposable,T>,T>>
    Shared<T> GetSharedStatic();

    template<typename T,typename = std::enable_if<std::is_base_of_v<Disposable,T>,T>>
    Weak<T> GetWeakStatic();
    
    
    Shared<Disposable> GetShared();
    
    Weak<Disposable> GetWeak();
        
};

template <typename T, typename>
Shared<T> Disposable::GetSharedDynamic()
{
    return std::dynamic_pointer_cast<T>(shared_from_this());
}

template <typename T, typename>
Weak<T> Disposable::GetWeakDynamic()
{
    return std::dynamic_pointer_cast<T>(weak_from_this().lock());
}

template <typename T, typename>
Shared<T> Disposable::GetSharedStatic()
{
    return std::static_pointer_cast<T>(shared_from_this());
}

template <typename T, typename>
Weak<T> Disposable::GetWeakStatic()
{
    return std::static_pointer_cast<T>(weak_from_this().lock());
}
