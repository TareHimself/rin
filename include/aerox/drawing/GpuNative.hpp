#pragma once

namespace aerox::drawing {
class GpuNative {
public:
  virtual bool IsUploaded() const = 0;
  virtual void Upload() = 0;
};
}
