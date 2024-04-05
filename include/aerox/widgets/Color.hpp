#pragma once
#include <glm/vec4.hpp>

namespace aerox::widgets {
class Color {
protected:
  glm::vec4 _color{0.0f,0.0f,0.0f,0.0f};
public:

  template<typename ...TArgs,typename = std::enable_if_t<std::is_constructible_v<glm::vec4,TArgs...>>>
  Color(TArgs&&... args);

  operator glm::vec4();
  operator glm::vec4() const;

  
};

template <typename ... TArgs, typename> Color::Color(TArgs &&... args) {
  _color = glm::vec4(std::forward<TArgs>(args)...);
}
}
