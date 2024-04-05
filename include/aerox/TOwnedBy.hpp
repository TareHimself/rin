#pragma once
#include <aerox/TObjectWithInit.hpp>
namespace  aerox {

    // TObjectWithInit specialization for objects that are owned by other objects i.e.
    template<typename TOwner,typename ...TArgs>
    class TOwnedBy : public TObjectWithInit<TOwner *,TArgs...> {
        TOwner * _owner = nullptr;

    public:

        TOwner * GetOwner() const {
            return _owner;
        }

        virtual void OnInit(TOwner * owner,TArgs... args){
            _owner = owner;
        }
    };
}