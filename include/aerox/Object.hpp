#pragma once
#include "typedefs.hpp"
#include "log.hpp"
#include "types.hpp"
#include "containers/TDelegate.hpp"
#include "meta/IMetadata.hpp"
#include "gen/Object.gen.hpp"
namespace aerox {

  struct WithCleanupQueue {
  protected:
    CleanupQueue _cleaner;
  public:
    void AddCleanup(const std::function<void()> &callback);

    template <typename ...T>
    void AddCleanup(std::shared_ptr<TDelegateHandle<T...>> handle);

    void Clean();
  };

  template <typename ... T> void WithCleanupQueue::AddCleanup(std::shared_ptr<TDelegateHandle<T...>> handle) {
    _cleaner.Push([handle] {
      handle->UnBind();
    });
  }

  inline void WithCleanupQueue::AddCleanup(const std::function<void()> &callback) {
    _cleaner.Push(callback);
  }

  inline void WithCleanupQueue::Clean() {
    _cleaner.Run();
  }


  // Base class for dynamically allocated objects
  META_TYPE()
  class Object : public WithCleanupQueue, public std::enable_shared_from_this<Object>, public meta::IMetadata {
    
    std::string _instanceId;
    bool _destroyPending = false;
  public:

    META_BODY()
    
    std::string GetObjectInstanceId() const;

    DECLARE_DELEGATE(onDestroyedDelegate)

    Object();

    virtual ~Object();

    virtual void FinishDestroy();

    void Destroy();
    
    virtual void OnDestroy();

    bool IsPendingDestroy() const;

    META_FUNCTION()
    json ToJson() override;
  };

template <typename T, typename... Args>
TSharedConstruct<T,Args...> newObject(Args&&... args) {
    static_assert(std::is_base_of_v<Object, T>, "T must be a child of Object");
  
  return std::shared_ptr<T>(new T(std::forward<Args>(args)...),[](T * ptr) {
        static_cast<Object *>(ptr)->Destroy();
    });
  }
}

