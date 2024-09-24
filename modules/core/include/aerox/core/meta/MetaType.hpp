#pragma once
#include <string>
#include <typeindex>
#include "MetaMacros.hpp"
class MetaType {
    std::string _id{};
    size_t _size{0};

public:
    MetaType();
    MetaType(const std::string& id,const size_t& size);
    template<typename T>
    static MetaType Infer();
    [[nodiscard]] std::string GetId() const;
    [[nodiscard]] size_t GetSize() const;
};

template <typename T>
MetaType MetaType::Infer()
{
    const std::type_index typeIndex = typeid(T);
    return MetaType{typeIndex.name(),sizeof(T)};
}
