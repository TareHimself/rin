#pragma once

namespace vengine::drawing {
class GpuNative {
public:
  virtual bool IsUploaded() const = 0;
  virtual void Upload() = 0;
};
}
