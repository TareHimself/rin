#pragma once

#include <cmath>

consteval int flagField(int index){
    return 1 << index;
}

template<typename T,typename E = uint32_t,typename = std::enable_if_t<std::is_enum_v<T>>>
class TFlags {
    E _flags;
public:

    TFlags() {
        _flags = 0;
    }


    explicit TFlags(E flags) {
        _flags = flags;
    }

    explicit TFlags(T flags) {
        _flags = static_cast<E>(flags);
    }

    bool Has(const E& flag) const{
        return _flags & flag;
    }

    bool Has(const T& flag) const{
        return _flags & static_cast<E>(flag);
    }

    TFlags& operator|=(const E& flag){
        _flags |= flag;
        return *this;
    }

    TFlags& operator|=(const T& flag){
        _flags |= static_cast<E>(flag);
        return *this;
    }

    TFlags& operator&=(const E& flag){
        _flags &= ~flag;
        return *this;
    }

    TFlags& operator&=(const T& flag){
        _flags &= ~static_cast<E>(flag);
        return *this;
    }

    TFlags& Add(const E& flag){
        *this |= flag;
        return *this;
    }

    TFlags& Add(const T& flag){
        *this |= static_cast<E>(flag);
        return *this;
    }

    TFlags& Remove(const E& flag){
        *this &= flag;
        return *this;
    }

    TFlags& Remove(const T& flag){
        *this &= static_cast<E>(flag);
        return *this;
    }

    explicit operator E() const{
        return _flags;
    }

};
