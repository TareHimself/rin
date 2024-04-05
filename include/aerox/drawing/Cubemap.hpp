#pragma once
#include "DrawingSubsystem.hpp"
#include "aerox/Object.hpp"

namespace aerox::drawing {
class Cubemap : public Object {
  Array<Array<unsigned char>> _images;
  std::shared_ptr<AllocatedImage> _gpuData;
public:
  
};
}
