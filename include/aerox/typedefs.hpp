#pragma once
#include <memory>

namespace aerox {

template<typename T,typename ...Args>
using TSharedConstruct = std::enable_if_t<std::is_constructible_v<T,Args...>,std::shared_ptr<T>>;

template<typename T,typename ...Args>
using TWeakConstruct = std::enable_if_t<std::is_constructible_v<T,Args...>,std::weak_ptr<T>>;
}
